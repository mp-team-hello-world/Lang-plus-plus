using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antlr4.Runtime.Tree;

namespace Kotlin_plus_plus;

/// <summary>
/// Построитель IR-дерева для Python на основе сгенерированных классов PythonParser.
/// Грамматика не менялась: исправлены только ошибки перевода.
/// </summary>
public class PythonAstBuilder : PythonParserBaseVisitor<AstNode>
{
    private static readonly LiteralExpr Zero = new(LiteralKind.Int, "0");
    private static readonly LiteralExpr One = new(LiteralKind.Int, "1");

    // ---------- области видимости ----------
    // В Python переменная появляется при первом присваивании, в C++ её надо объявить.
    // Область — блок; у каждой функции своя "рамка", внешние локальные имена ей не принадлежат.

    private readonly List<HashSet<string>> _scopes = new() { new HashSet<string>() };
    private int _frameStart;

    private bool IsDeclared(string name)
    {
        for (int i = _scopes.Count - 1; i >= _frameStart; i--)
        {
            if (_scopes[i].Contains(name)) return true;
        }
        return false;
    }

    private void Declare(string name) => _scopes[_scopes.Count - 1].Add(name);

    private void PushScope() => _scopes.Add(new HashSet<string>());

    private void PopScope() => _scopes.RemoveAt(_scopes.Count - 1);

    // ---------- вспомогательные ----------

    private Expr ToExpr(IParseTree tree) => tree == null ? null : Visit(tree) as Expr;

    private Stmt ToStmt(IParseTree tree) => tree == null ? null : Visit(tree) as Stmt;

    private List<Stmt> BuildStatements(PythonParser.StatementsContext context)
    {
        var result = new List<Stmt>();
        if (context == null) return result;

        foreach (var stmtCtx in context.statement())
        {
            if (stmtCtx.compoundStatement() != null)
            {
                if (Visit(stmtCtx.compoundStatement()) is Stmt compound) result.Add(compound);
            }
            else if (stmtCtx.simpleStatement() != null)
            {
                result.AddRange(SmallStatements(stmtCtx.simpleStatement()));
            }
        }
        return result;
    }

    // a = 1; b = 2  — каждая часть становится отдельной инструкцией (раньше оставалась только первая)
    private List<Stmt> SmallStatements(PythonParser.SimpleStatementContext context)
    {
        var result = new List<Stmt>();
        foreach (var small in context.smallStatement())
        {
            var node = Visit(small);
            if (node is Stmt stmt) result.Add(stmt);
            else if (node is Expr expr) result.Add(new ExprStmt(expr));
        }
        return result;
    }

    private static TypeRef ParseType(string typeName) => typeName switch
    {
        "int" => new TypeRef(DataType.Int, typeName),
        "bool" => new TypeRef(DataType.Bool, typeName),
        "float" => new TypeRef(DataType.Double, typeName),   // float в Python — это double
        "str" => new TypeRef(DataType.String, typeName),
        _ => new TypeRef(DataType.Unknown, typeName)
    };

    // ---------- корень программы ----------

    public override AstNode VisitRoot(PythonParser.RootContext context)
    {
        var result = new List<Stmt>();

        foreach (var stmt in BuildStatements(context.statements()))
        {
            // if __name__ == "__main__": ...  — тело становится кодом верхнего уровня
            if (IsMainGuard(stmt, out var body)) result.AddRange(body);
            else result.Add(stmt);
        }

        // def main(): ... и вызов main() на верхнем уровне: сам вызов не нужен, main и есть точка входа
        bool hasMain = result.OfType<FunctionDecl>().Any(f => f.Name == "main");
        if (hasMain)
        {
            result.RemoveAll(s => s is ExprStmt { Expression: CallExpr { Receiver: null, Name: "main" } call }
                                  && call.Args.Count == 0);
        }

        return new ProgramNode(result);
    }

    private static bool IsMainGuard(Stmt stmt, out List<Stmt> body)
    {
        body = null;
        if (stmt is IfStmt
            {
                Else: null,
                Condition: BinaryExpr
                {
                    Op: "==",
                    Left: IdentifierExpr { Name: "__name__" },
                    Right: LiteralExpr { Kind: LiteralKind.String } literal
                }
            } guard
            && literal.Text.Contains("__main__"))
        {
            body = guard.Then is BlockStmt block ? block.Statements.ToList() : new List<Stmt> { guard.Then };
            return true;
        }
        return false;
    }

    // ---------- инструкции ----------

    public override AstNode VisitStatement(PythonParser.StatementContext context)
    {
        if (context.compoundStatement() != null) return Visit(context.compoundStatement());
        if (context.simpleStatement() != null) return SmallStatements(context.simpleStatement()).FirstOrDefault();
        return null;
    }

    public override AstNode VisitCompoundStatement(PythonParser.CompoundStatementContext context)
    {
        if (context.functionDef() != null) return Visit(context.functionDef());
        if (context.ifStatement() != null) return Visit(context.ifStatement());
        if (context.forStatement() != null) return Visit(context.forStatement());
        if (context.whileStatement() != null) return Visit(context.whileStatement());
        if (context.tryStatement() != null) return Visit(context.tryStatement());
        return null;
    }

    public override AstNode VisitSmallStatement(PythonParser.SmallStatementContext context)
    {
        if (context.assignment() != null) return Visit(context.assignment());
        if (context.typeDeclaration() != null) return Visit(context.typeDeclaration());
        if (context.jumpStatement() != null) return Visit(context.jumpStatement());
        if (context.PASS() != null) return null; // pass пропускаем
        if (context.expression() != null)
        {
            var node = Visit(context.expression());
            if (node is Stmt stmt) return stmt;
            if (node is Expr expr) return new ExprStmt(expr);
        }
        if (context.unparsedStatement() != null) return Visit(context.unparsedStatement());
        return null;
    }

    // ---------- присваивания и объявления ----------

    // Первое присваивание имени — это объявление (auto x = ...), дальше — обычное присваивание
    private AstNode Assign(string name, string op, Expr value)
    {
        if (op == "=" && !IsDeclared(name))
        {
            Declare(name);
            return new VarDeclStmt(false, name, value);
        }
        return new AssignStmt(name, op, value);
    }

    public override AstNode VisitNormalAssignment(PythonParser.NormalAssignmentContext context)
        => Assign(context.ID().GetText(), "=", ToExpr(context.expression()));

    public override AstNode VisitAddAssignment(PythonParser.AddAssignmentContext context)
        => Assign(context.ID().GetText(), "+=", ToExpr(context.expression()));

    public override AstNode VisitSubAssignment(PythonParser.SubAssignmentContext context)
        => Assign(context.ID().GetText(), "-=", ToExpr(context.expression()));

    public override AstNode VisitMultAssignment(PythonParser.MultAssignmentContext context)
        => Assign(context.ID().GetText(), "*=", ToExpr(context.expression()));

    public override AstNode VisitDivAssignment(PythonParser.DivAssignmentContext context)
        => Assign(context.ID().GetText(), "/=", ToExpr(context.expression()));

    // x: int = 5  /  x: int — тип теперь сохраняется (раньше терялся и получалось "auto x;")
    public override AstNode VisitTypeDeclaration(PythonParser.TypeDeclarationContext context)
    {
        string name = context.ID().GetText();
        var initExpr = context.expression() != null ? ToExpr(context.expression()) : null;
        Declare(name);
        return new VarDeclStmt(false, name, initExpr, ParseType(context.type().GetText()));
    }

    // ---------- управляющие конструкции ----------

    public override AstNode VisitIfStatement(PythonParser.IfStatementContext context)
    {
        var exprs = context.expression();
        var blocks = context.block();

        // Рекурсивная сборка цепочки if -> else if (elif) -> else
        Stmt BuildIfChain(int index)
        {
            if (index >= exprs.Length)
            {
                // Если кончились условия, значит остался блок else (если он есть)
                return blocks.Length > index ? ToStmt(blocks[index]) : null;
            }

            var condition = ToExpr(exprs[index]);
            var thenBranch = ToStmt(blocks[index]);
            var elseBranch = BuildIfChain(index + 1);

            return new IfStmt(condition, thenBranch, elseBranch);
        }

        return BuildIfChain(0);
    }

    public override AstNode VisitWhileStatement(PythonParser.WhileStatementContext context)
    {
        return new WhileStmt(ToExpr(context.expression()), ToStmt(context.block()));
    }

    // range(n) / range(a, b) / range(a, b, step) / for x in <выражение>
    public override AstNode VisitForStatement(PythonParser.ForStatementContext context)
    {
        string varName = context.ID().GetText();
        var exprCtx = context.expression();

        PushScope();
        Declare(varName);
        try
        {
            if (exprCtx is PythonParser.FunctionCallContext callCtx
                && callCtx.expression(0) is PythonParser.IdentifierContext callee
                && callee.ID().GetText() == "range")
            {
                // expression(0) — сам range, дальше его аргументы
                var args = callCtx.expression().Skip(1).Select(ToExpr).ToList();
                Stmt body = ToStmt(context.block());

                // В IR конец ForRange включительный, а в Python range конец исключает: end - 1
                if (args.Count == 1) return new ForRangeStmt(varName, Zero, MinusOne(args[0]), body);
                if (args.Count == 2) return new ForRangeStmt(varName, args[0], MinusOne(args[1]), body);

                if (args.Count == 3)
                {
                    // range(a, b, step): шаг больше не теряется
                    Expr step = args[2];
                    bool negative = step is UnaryExpr { Op: "-" };

                    var init = new VarDeclStmt(false, varName, args[0], new TypeRef(DataType.Int, "int"));
                    var condition = new BinaryExpr(negative ? ">" : "<", new IdentifierExpr(varName), args[1]);
                    return new ForStmt(init, condition, new AssignStmt(varName, "+=", step), body);
                }
            }

            // for x in строка_или_коллекция — обход элементов (раньше выражение считалось верхней границей)
            return new ForEachStmt(varName, null, ToExpr(exprCtx), ToStmt(context.block()));
        }
        finally
        {
            PopScope();
        }
    }

    private static Expr MinusOne(Expr end)
    {
        if (end is LiteralExpr { Kind: LiteralKind.Int } literal && int.TryParse(literal.Text, out int n))
            return new LiteralExpr(LiteralKind.Int, (n - 1).ToString());

        Expr left = end is IdentifierExpr or LiteralExpr ? end : new ParenExpr(end);
        return new BinaryExpr("-", left, One);
    }

    public override AstNode VisitFunctionDef(PythonParser.FunctionDefContext context)
    {
        string name = context.ID().GetText();

        // У функции своя рамка: внешние локальные переменные ей не принадлежат
        int savedFrame = _frameStart;
        _frameStart = _scopes.Count;
        PushScope();

        var parameters = new List<Parameter>();
        if (context.parameterList() != null)
        {
            foreach (var p in context.parameterList().parameter())
            {
                string pName = p.ID().GetText();
                string pType = p.type() != null ? p.type().GetText() : "int";

                // значение по умолчанию (def f(x=1)) теперь сохраняется
                parameters.Add(new Parameter(pName, ParseType(pType), ToExpr(p.expression())));
                Declare(pName);
            }
        }

        TypeRef returnType = context.type() != null ? ParseType(context.type().GetText()) : TypeRef.Void;
        var body = ToStmt(context.block()) as BlockStmt ?? new BlockStmt(new List<Stmt>());

        PopScope();
        _frameStart = savedFrame;

        // Передаем IsExpressionBody = false, так как функции в Python имеют блок с телом
        return new FunctionDecl(name, parameters, returnType, body, IsExpressionBody: false);
    }

    public override AstNode VisitBlock(PythonParser.BlockContext context)
    {
        PushScope();
        var statements = context.statements() != null
            ? BuildStatements(context.statements())
            : context.simpleStatement() != null
                ? SmallStatements(context.simpleStatement())
                : new List<Stmt>();
        PopScope();

        return new BlockStmt(statements);
    }

    public override AstNode VisitJumpStatement(PythonParser.JumpStatementContext context)
    {
        if (context.RETURN() != null) return new ReturnStmt(ToExpr(context.expression()));
        if (context.BREAK() != null) return new BreakStmt();
        if (context.CONTINUE() != null) return new ContinueStmt();
        if (context.RAISE() != null) return new ThrowStmt(ToExpr(context.expression()));
        return null;
    }

    public override AstNode VisitTryStatement(PythonParser.TryStatementContext context)
    {
        var body = ToStmt(context.block()) as BlockStmt;
        var catches = new List<CatchClause>();

        foreach (var cb in context.exceptBlock())
        {
            string exType = cb.type() != null ? cb.type().GetText() : "Exception";
            string exParam = cb.ID() != null ? cb.ID().GetText() : "e";
            catches.Add(new CatchClause(exParam, exType, ToStmt(cb.block()) as BlockStmt));
        }

        BlockStmt finallyBlock = context.finallyBlock() != null
            ? ToStmt(context.finallyBlock().block()) as BlockStmt
            : null;

        return new TryStmt(body, catches, finallyBlock);
    }

    // ---------- вызовы и обращения к членам (раньше передавались сырым текстом) ----------

    public override AstNode VisitFunctionCall(PythonParser.FunctionCallContext context)
    {
        var all = context.expression();
        var args = all.Skip(1).Select(ToExpr).ToList();

        if (all[0] is PythonParser.MemberAccessContext member)
            return new CallExpr(ToExpr(member.expression()), member.ID().GetText(), args);

        if (all[0] is PythonParser.IdentifierContext id)
            return BuildFreeCall(id.ID().GetText(), args);

        return new RawExpr(AntlrText.Raw(context));
    }

    // Имена встроенных функций Python -> имена, которые знает C++ backend.
    // Остальные вызовы (свои функции, abs, min, max, round ...) идут как есть.
    private static Expr BuildFreeCall(string name, List<Expr> args)
    {
        switch (name)
        {
            case "print": return BuildPrint(args);
            case "input": return new CallExpr(null, "readLine", new List<Expr>());
            case "len" when args.Count == 1: return new MemberExpr(args[0], "size");
            case "str" when args.Count == 1: return new CallExpr(args[0], "toString", new List<Expr>());
            case "int" when args.Count == 1: return new CallExpr(args[0], "toInt", new List<Expr>());
            case "float" when args.Count == 1: return new CallExpr(args[0], "toDouble", new List<Expr>());
        }
        return new CallExpr(null, name, args);
    }

    // print(a, b, c) печатает аргументы через пробел
    private static Expr BuildPrint(List<Expr> args)
    {
        if (args.Count <= 1) return new CallExpr(null, "println", args);

        var parts = new List<Expr>();
        for (int i = 0; i < args.Count; i++)
        {
            if (i > 0) parts.Add(new LiteralExpr(LiteralKind.String, "\" \""));
            parts.Add(args[i]);
        }
        return new CallExpr(null, "println", new List<Expr> { new TemplateExpr(parts) });
    }

    public override AstNode VisitMemberAccess(PythonParser.MemberAccessContext context)
        => new MemberExpr(ToExpr(context.expression()), context.ID().GetText());

    // ---------- выражения ----------

    public override AstNode VisitParens(PythonParser.ParensContext context)
        => new ParenExpr(ToExpr(context.expression()));

    public override AstNode VisitUnaryMinus(PythonParser.UnaryMinusContext context)
        => new UnaryExpr("-", ToExpr(context.expression()));

    public override AstNode VisitUnaryNot(PythonParser.UnaryNotContext context)
        => new UnaryExpr("!", ToExpr(context.expression()));

    public override AstNode VisitMulDiv(PythonParser.MulDivContext context)
    {
        string op = context.GetChild(1).GetText();
        // // в C++ — комментарий; целочисленное деление там записывается как /
        if (op == "//") op = "/";
        return new BinaryExpr(op, ToExpr(context.expression(0)), ToExpr(context.expression(1)));
    }

    public override AstNode VisitAddSub(PythonParser.AddSubContext context)
        => new BinaryExpr(context.GetChild(1).GetText(), ToExpr(context.expression(0)), ToExpr(context.expression(1)));

    public override AstNode VisitComparison(PythonParser.ComparisonContext context)
        => new BinaryExpr(context.GetChild(1).GetText(), ToExpr(context.expression(0)), ToExpr(context.expression(1)));

    public override AstNode VisitAndLogical(PythonParser.AndLogicalContext context)
        => new BinaryExpr("&&", ToExpr(context.expression(0)), ToExpr(context.expression(1)));

    public override AstNode VisitOrLogical(PythonParser.OrLogicalContext context)
        => new BinaryExpr("||", ToExpr(context.expression(0)), ToExpr(context.expression(1)));

    public override AstNode VisitIdentifier(PythonParser.IdentifierContext context)
        => new IdentifierExpr(context.ID().GetText());

    public override AstNode VisitIntLiteral(PythonParser.IntLiteralContext context)
        => new LiteralExpr(LiteralKind.Int, context.INT().GetText());

    public override AstNode VisitFloatLiteral(PythonParser.FloatLiteralContext context)
        => new LiteralExpr(LiteralKind.Double, context.FLOAT().GetText());

    public override AstNode VisitStringLiteral(PythonParser.StringLiteralContext context)
        => new LiteralExpr(LiteralKind.String, NormalizeString(context.STRING().GetText()));

    public override AstNode VisitTrueLiteral(PythonParser.TrueLiteralContext context)
        => new LiteralExpr(LiteralKind.Int, "true");

    public override AstNode VisitFalseLiteral(PythonParser.FalseLiteralContext context)
        => new LiteralExpr(LiteralKind.Int, "false");

    public override AstNode VisitNoneLiteral(PythonParser.NoneLiteralContext context)
        => new LiteralExpr(LiteralKind.Int, "nullptr");

    public override AstNode VisitUnparsedStatement(PythonParser.UnparsedStatementContext context)
        => new UnsupportedStmt("Unparsed python statement", AntlrText.Raw(context));

    // 'abc' -> "abc": в C++ одинарные кавычки — это символ, а не строка
    private static string NormalizeString(string token)
    {
        string inner = token.Substring(1, token.Length - 2);
        var sb = new StringBuilder("\"");

        for (int i = 0; i < inner.Length; i++)
        {
            char c = inner[i];
            if (c == '\\' && i + 1 < inner.Length)
            {
                if (inner[i + 1] == '\'') sb.Append('\'');     // \' внутри двойных кавычек не нужен
                else sb.Append(c).Append(inner[i + 1]);
                i++;
            }
            else if (c == '"') sb.Append("\\\"");              // " внутри 'строки' надо экранировать
            else sb.Append(c);
        }

        return sb.Append('"').ToString();
    }
}