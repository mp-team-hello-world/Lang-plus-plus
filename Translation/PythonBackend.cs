/*using System.Collections.Generic;
using System.Linq;

namespace Kotlin_plus_plus;

/// <summary>
/// IR -> Python. ПРИМЕР того, как добавляется новый целевой язык: один файл,
/// никакого копирования разбора Kotlin. Заметно отличается от C++ (отступы вместо
/// фигурных скобок, elif, range, and/or/not, настоящий finally), но IR тот же самый.
///
/// Известные упрощения: Kotlin-деление целых (/) в Python даёт float; continue внутри
/// do-while пропускает проверку условия; вызовы (RawExpr) остаются в синтаксисе Kotlin.
/// </summary>
public sealed class PythonBackend : ITargetBackend
{
    public string Generate(ProgramNode program) => new Generator().Generate(program);

    private sealed class Generator : IStmtVisitor, IExprVisitor<string>
    {
        private readonly CodeWriter _w = new();

        public string Generate(ProgramNode program)
        {
            var functions = program.Statements.OfType<FunctionDecl>().ToList();

            // В Python функцию нужно определить до вызова — функции идут первыми
            foreach (var function in functions) Emit(function);

            foreach (var stmt in program.Statements)
            {
                if (stmt is not FunctionDecl) Emit(stmt);
            }

            if (functions.Any(f => f.Name == "main"))
            {
                _w.Line("if __name__ == \"__main__\":");
                _w.Indent();
                _w.Line("main()");
                _w.Dedent();
            }

            return string.Join("\n", _w.Lines);
        }

        private void Emit(Stmt stmt) => stmt?.Accept(this);

        private string Render(Expr expr) => expr == null ? string.Empty : expr.Accept(this);

        // Тело блока в Python — это отступ; пустое тело обязано содержать pass
        private void EmitBody(Stmt body)
        {
            _w.Indent();
            int before = _w.Count;

            if (body is BlockStmt block)
            {
                foreach (var stmt in block.Statements) Emit(stmt);
            }
            else
            {
                Emit(body);
            }

            if (_w.Count == before) _w.Line("pass");
            _w.Dedent();
        }

        private static string PyType(TypeRef type) => type.Kind switch
        {
            DataType.Int => "int",
            DataType.Long => "int",
            DataType.Float => "float",
            DataType.Double => "float",
            DataType.Bool => "bool",
            DataType.Char => "str",
            DataType.String => "str",
            DataType.Void => "None",
            _ => "object"
        };

        private static string PyException(string kotlinType) => kotlinType switch
        {
            "IOException" => "OSError",
            _ => "Exception"
        };

        // ---------- выражения ----------

        public string VisitLiteral(LiteralExpr node) =>
            node.Kind == LiteralKind.Bool ? (node.Text == "true" ? "True" : "False") : node.Text;

        public string VisitIdentifier(IdentifierExpr node) => node.Name;

        public string VisitParen(ParenExpr node) => $"({Render(node.Inner)})";

        public string VisitUnary(UnaryExpr node) =>
            node.Op == "!" ? $"not {Render(node.Operand)}" : $"{node.Op}{Render(node.Operand)}";

        public string VisitBinary(BinaryExpr node)
        {
            string op = node.Op switch
            {
                "&&" => "and",
                "||" => "or",
                _ => node.Op
            };
            return $"{Render(node.Left)} {op} {Render(node.Right)}";
        }

        public string VisitRaw(RawExpr node) => node.SourceText;

        // ---------- инструкции ----------

        public void VisitExprStmt(ExprStmt node)
        {
            string text = Render(node.Expression);
            if (!string.IsNullOrEmpty(text)) _w.Line(text);
        }

        // int x;  ->  x = None  (в Python переменную нельзя объявить без значения)
        public void VisitVarDecl(VarDeclStmt node) =>
            _w.Line(node.Init == null ? $"{node.Name} = None" : $"{node.Name} = {Render(node.Init)}");

        public void VisitIncDec(IncDecStmt node) =>
            _w.Line(node.IsIncrement ? $"{node.Target} += 1" : $"{node.Target} -= 1");

        public void VisitAssign(AssignStmt node) => _w.Line($"{node.Target} {node.Op} {Render(node.Value)}");

        // В Python нет блоков с собственной областью видимости — выводим содержимое как есть
        public void VisitBlock(BlockStmt node)
        {
            foreach (var stmt in node.Statements) Emit(stmt);
        }

        public void VisitIf(IfStmt node) => EmitIf("if", node);

        private void EmitIf(string keyword, IfStmt node)
        {
            _w.Line($"{keyword} {Render(node.Condition)}:");
            EmitBody(node.Then);

            if (node.Else is IfStmt elseIf)
            {
                EmitIf("elif", elseIf);
            }
            else if (node.Else != null)
            {
                _w.Line("else:");
                EmitBody(node.Else);
            }
        }

        // Kotlin 1..n включает n, а range() — нет: нужен конец + 1
        private string PlusOne(Expr end) =>
            end is LiteralExpr { Kind: LiteralKind.Int } literal && long.TryParse(literal.Text, out long value)
                ? (value + 1).ToString()
                : $"{Render(end)} + 1";

        public void VisitForRange(ForRangeStmt node)
        {
            _w.Line($"for {node.Variable} in range({Render(node.Start)}, {PlusOne(node.EndInclusive)}):");
            EmitBody(node.Body);
        }

        public void VisitFor(ForStmt node)
        {
            if (TryEmitForAsRange(node)) return;

            // Общий случай: init; while cond: body; update
            // (известное упрощение: continue в теле пропустит update)
            Emit(node.Init);
            string condition = node.Condition == null ? "True" : Render(node.Condition);
            _w.Line($"while {condition}:");
            _w.Indent();
            int before = _w.Count;
            EmitStatements(node.Body);
            Emit(node.Update);
            if (_w.Count == before) _w.Line("pass");
            _w.Dedent();
        }

        // for (int i = a; i < b; i++)  ->  for i in range(a, b)   (<= даёт b + 1)
        private bool TryEmitForAsRange(ForStmt node)
        {
            if (node.Init is not VarDeclStmt init || init.Init == null) return false;
            if (node.Condition is not BinaryExpr cond) return false;
            if (cond.Left is not IdentifierExpr condVar || condVar.Name != init.Name) return false;
            if (cond.Op != "<" && cond.Op != "<=") return false;
            if (node.Update is not IncDecStmt { IsIncrement: true } step || step.Target != init.Name) return false;

            string end = cond.Op == "<" ? Render(cond.Right) : PlusOne(cond.Right);
            _w.Line($"for {init.Name} in range({Render(init.Init)}, {end}):");
            EmitBody(node.Body);
            return true;
        }

        public void VisitForEach(ForEachStmt node)
        {
            _w.Line($"for {node.Variable} in {Render(node.Iterable)}:");
            EmitBody(node.Body);
        }

        // Содержимое тела без отступа и без pass (для тел, к которым дописываются ещё строки)
        private void EmitStatements(Stmt body)
        {
            if (body is BlockStmt block)
            {
                foreach (var stmt in block.Statements) Emit(stmt);
            }
            else
            {
                Emit(body);
            }
        }

        public void VisitWhile(WhileStmt node)
        {
            _w.Line($"while {Render(node.Condition)}:");
            EmitBody(node.Body);
        }

        public void VisitDoWhile(DoWhileStmt node)
        {
            // do { body } while (cond)  ==>  while True: body; if not cond: break
            _w.Line("while True:");
            _w.Indent();

            if (node.Body is BlockStmt block)
            {
                foreach (var stmt in block.Statements) Emit(stmt);
            }
            else
            {
                Emit(node.Body);
            }

            _w.Line($"if not ({Render(node.Condition)}):");
            _w.Indent();
            _w.Line("break");
            _w.Dedent();
            _w.Dedent();
        }

        public void VisitFunction(FunctionDecl node)
        {
            string parameters = string.Join(", ", node.Parameters.Select(p =>
                (p.Type.Kind is DataType.Unknown or DataType.Void)
                    ? p.Name
                    : $"{p.Name}: {PyType(p.Type)}"));

            string returns = node.ReturnType.Kind == DataType.Unknown ? "" : $" -> {PyType(node.ReturnType)}";

            _w.Line($"def {node.Name}({parameters}){returns}:");
            EmitBody(node.Body);
            _w.Line("");
        }

        public void VisitReturn(ReturnStmt node) =>
            _w.Line(node.Value == null ? "return" : $"return {Render(node.Value)}");

        public void VisitBreak(BreakStmt node) => _w.Line("break");

        public void VisitContinue(ContinueStmt node) => _w.Line("continue");

        public void VisitThrow(ThrowStmt node) => _w.Line($"raise {Render(node.Value)}");

        public void VisitTry(TryStmt node)
        {
            _w.Line("try:");
            EmitBody(node.Body);

            foreach (var clause in node.Catches)
            {
                _w.Line(string.IsNullOrEmpty(clause.ExceptionType)
                    ? "except:"
                    : $"except {PyException(clause.ExceptionType)} as {clause.ParamName}:");
                EmitBody(clause.Body);
            }

            if (node.Finally != null)
            {
                _w.Line("finally:");
                EmitBody(node.Finally);
            }
        }

        // pass, а не просто комментарий: иначе пустое тело сломало бы синтаксис Python
        public void VisitUnsupported(UnsupportedStmt node) =>
            _w.Line($"pass  # {node.Description}: {node.SourceText}");
    }
}
*/