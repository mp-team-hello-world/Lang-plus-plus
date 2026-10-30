using System;
using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime;

namespace Kotlin_plus_plus;

public abstract class PythonLexerBase : Lexer
{
    private readonly Stack<int> _indentStack = new();
    private readonly Queue<IToken> _pendingTokens = new();
    private int _openedBrackets = 0;
    private IToken? _lastToken = null;

    // Позиция последнего символа последнего настоящего токена. Нужна, чтобы у искусственных
    // INDENT/DEDENT были осмысленные границы в исходном тексте (иначе дерево разбора показывает мусор).
    private int _lastRealStop = -1;

    protected PythonLexerBase(ICharStream input) : base(input)
    {
        Init();
    }

    protected PythonLexerBase(ICharStream input, TextWriter output, TextWriter errorOutput) : base(input, output, errorOutput)
    {
        Init();
    }

    private void Init()
    {
        _indentStack.Clear();
        _indentStack.Push(0);
        _pendingTokens.Clear();
        _openedBrackets = 0;
        _lastToken = null;
        _lastRealStop = -1;
    }

    public override void Reset()
    {
        Init();
        base.Reset();
    }

    public override IToken NextToken()
    {
        if (_pendingTokens.Count > 0)
        {
            var pending = _pendingTokens.Dequeue();
            _lastToken = pending;
            return pending;
        }

        var next = base.NextToken();
        if (next.Type != Eof) _lastRealStop = next.StopIndex;

        // Отслеживаем открытые скобки () [] {}, внутри которых переводы строк игнорируются
        if (next.Text == "(" || next.Text == "[" || next.Text == "{")
        {
            _openedBrackets++;
        }
        else if (next.Text == ")" || next.Text == "]" || next.Text == "}")
        {
            if (_openedBrackets > 0) _openedBrackets--;
        }

        // Если это NEWLINE и мы вне скобок — вычисляем отступ следующей строки
        if (next.Type == PythonLexer.NEWLINE && _openedBrackets == 0)
        {
            ProcessNewLineAndIndentation(next);
            if (_pendingTokens.Count > 0)
            {
                var token = _pendingTokens.Dequeue();
                _lastToken = token;
                return token;
            }
        }
        else if (next.Type == Eof)
        {
            // В конце файла закрываем все незакрытые отступы DEDENT-токенами
            while (_indentStack.Count > 1)
            {
                _indentStack.Pop();
                _pendingTokens.Enqueue(CreateDedentToken());
            }

            if (_pendingTokens.Count > 0)
            {
                _pendingTokens.Enqueue(next);
                var token = _pendingTokens.Dequeue();
                _lastToken = token;
                return token;
            }
        }

        _lastToken = next;
        return next;
    }

    private void ProcessNewLineAndIndentation(IToken newlineToken)
    {
        _pendingTokens.Enqueue(newlineToken);

        // Пропускаем пустые строки и комментарии, подсчитывая отступ следующей значащей строки
        int nextChar = InputStream.LA(1);
        int indent = 0;

        while (nextChar != Eof)
        {
            // Пропуск пробелов
            indent = 0;
            while (nextChar == ' ' || nextChar == '\t')
            {
                indent += (nextChar == '\t') ? 8 : 1;
                InputStream.Consume();
                nextChar = InputStream.LA(1);
            }

            // Если пустая строка (перенос строки)
            if (nextChar == '\r' || nextChar == '\n')
            {
                if (nextChar == '\r' && InputStream.LA(2) == '\n')
                {
                    InputStream.Consume();
                }
                InputStream.Consume();
                nextChar = InputStream.LA(1);
                continue;
            }

            // Если комментарий
            if (nextChar == '#')
            {
                while (nextChar != '\r' && nextChar != '\n' && nextChar != Eof)
                {
                    InputStream.Consume();
                    nextChar = InputStream.LA(1);
                }
                continue;
            }

            break;
        }

        if (nextChar == Eof)
        {
            return;
        }

        int currentIndent = _indentStack.Peek();
        if (indent > currentIndent)
        {
            _indentStack.Push(indent);
            _pendingTokens.Enqueue(CreateIndentToken());
        }
        else if (indent < currentIndent)
        {
            while (_indentStack.Count > 1 && indent < _indentStack.Peek())
            {
                _indentStack.Pop();
                _pendingTokens.Enqueue(CreateDedentToken());
            }
        }
    }

    private CommonToken CreateIndentToken()
    {
        var token = new CommonToken(PythonLexer.INDENT, "<INDENT>");
        token.Line = Line;
        token.Column = Column;
        SetEmptySpan(token);
        return token;
    }

    private CommonToken CreateDedentToken()
    {
        var token = new CommonToken(PythonLexer.DEDENT, "<DEDENT>");
        token.Line = Line;
        token.Column = Column;
        SetEmptySpan(token);
        return token;
    }

    // Пустой отрезок сразу после последнего настоящего токена (Stop = Start - 1, как у пустых токенов в ANTLR).
    // Тогда правило, которое заканчивается на DEDENT, охватывает текст ровно до последнего настоящего токена.
    private void SetEmptySpan(CommonToken token)
    {
        token.StartIndex = _lastRealStop + 1;
        token.StopIndex = _lastRealStop;
    }
}