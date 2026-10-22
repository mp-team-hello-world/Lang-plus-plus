using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace Kotlin_plus_plus;

/// <summary>
/// Превращает дерево ANTLR (грамматика Kotlin) в язык-независимое IR.
/// Здесь живёт ВСЁ, что связано с синтаксисом Kotlin; ничего про C++ или Python.
/// </summary>
public class KotlinAstBuilder : KotlinParserBaseVisitor<AstNode>
{
    private static readonly LiteralExpr Zero = new(LiteralKind.Int, "0");
    private static readonly LiteralExpr One = new(LiteralKind.Int, "1");

    // ---------- вспомогательные ----------

    private Expr ToExpr(IParseTree tree) => tree == null ? null : Visit(tree) as Expr;

    private Stmt ToStmt(IParseTree tree) => tree == null ? null : Visit(tree) as Stmt;

    private List<Stmt> BuildStatements(KotlinParser.StatementsContext statements)
    {
        var result = new List<Stmt>();
        if (statements == null) return result;

        foreach (var statement in statements.statement())
        {
            if (Visit(statement) is Stmt stmt) result.Add(stmt);
        }
        return result;
    }

    private List<Expr> ToExprs(IEnumerable<IParseTree> trees) => trees.Select(ToExpr).ToList();

    private static TypeRef ToTypeRef(KotlinParser.KotlinTypeContext ctx)
    {
        if (ctx == null) return null;

        string name = ctx.ID().GetText();
        List<TypeRef> args = ctx.typeArguments() == null
            ? null
            : ctx.typeArguments().kotlinType().Select(ToTypeRef).ToList();

        return MakeType(name, args);
    }

    private static TypeRef MakeType(string name, List<TypeRef> args)
    {
        switch (name)
        {
            case "Int": return new TypeRef(DataType.Int, name);
            case "Boolean": return new TypeRef(DataType.Bool, name);
            case "Long": return new TypeRef(DataType.Long, name);
            case "Unit": return new TypeRef(DataType.Void, name);
            case "Char": return new TypeRef(DataType.Char, name);
            case "Float": return new TypeRef(DataType.Float, name);
            case "Double": return new TypeRef(DataType.Double, name);
            case "String": return new TypeRef(DataType.String, name);

            case "List":
            case "MutableList":
            case "ArrayList":
            case "Collection":
            case "MutableCollection":
            case "Array":
                return new TypeRef(DataType.List, name, args);

            case "IntArray": return new TypeRef(DataType.List, name, new List<TypeRef> { new(DataType.Int, "Int") });
            case "LongArray": return new TypeRef(DataType.List, name, new List<TypeRef> { new(DataType.Long, "Long") });
            case "DoubleArray": return new TypeRef(DataType.List, name, new List<TypeRef> { new(DataType.Double, "Double") });
            case "BooleanArray": return new TypeRef(DataType.List, name, new List<TypeRef> { new(DataType.Bool, "Boolean") });
            case "CharArray": return new TypeRef(DataType.List, name, new List<TypeRef> { new(DataType.Char, "Char") });

            default: return new TypeRef(DataType.Unknown, name);
        }
    }

    private static readonly TypeRef NoType = new(DataType.Unknown, "");

    // ---------- корень и инструкции ----------

    // root: statements EOF;
    public override AstNode VisitRoot(KotlinParser.RootContext context)
    {
        return new ProgramNode(BuildStatements(context.statements()));
    }

    public override AstNode VisitStatement(KotlinParser.StatementContext context)
    {
        if (context.expression() != null)
        {
            var node = Visit(context.expression());

            // repeat(n) { ... } как инструкция -> обычный цикл
            if (node is CallExpr call && TryLowerRepeat(call) is Stmt loop) return loop;

            if (node is Stmt stmt) return stmt;               // на всякий случай
            if (node is Expr expr) return new ExprStmt(expr); // обычное выражение как инструкция
            return null;
        }

        // Остальные варианты (if, for, while, when, объявления, блок...) — ребёнок и есть инструкция
        for (int i = 0; i < context.ChildCount; i++)
        {
            if (Visit(context.GetChild(i)) is Stmt child) return child;
        }
        return null;
    }

    // repeat(n) { i -> ... }  ==>  for (i in 0..n-1)
    private static Stmt TryLowerRepeat(CallExpr call)
    {
        if (call.Receiver != null || call.Name != "repeat" || call.Args.Count != 2) return null;
        if (call.Args[1] is not LambdaExpr lambda) return null;

        string variable = lambda.Params.Count > 0 ? lambda.Params[0].Name : "it";
        Expr count = call.Args[0];
        if (count is not (IdentifierExpr or LiteralExpr)) count = new ParenExpr(count);

        return new ForRangeStmt(variable, Zero, new BinaryExpr("-", count, One), lambda.Body);
    }

    // variableDeclaration: (VAL | VAR) ID (COLON kotlinType)? (ASSIGNMENT NL* expression)?;
    public override AstNode VisitVariableDeclaration(KotlinParser.VariableDeclarationContext context)
    {
        if (context.expression() == null && context.kotlinType() == null)
        {
            return new UnsupportedStmt("Unsupported variable declaration syntax", AntlrText.Raw(context));
        }

        string name = context.ID()?.GetText() ?? "unknownVar";
        bool isConst = context.VAL() != null;
        return new VarDeclStmt(isConst, name, ToExpr(context.expression()), ToTypeRef(context.kotlinType()));
    }

    // ---------- присваивания ----------

    public override AstNode VisitNormalAssignment(KotlinParser.NormalAssignmentContext context)
        => new AssignStmt(context.ID().GetText(), "=", ToExpr(context.expression()));

    public override AstNode VisitAddAssignment(KotlinParser.AddAssignmentContext context)
        => new AssignStmt(context.ID().GetText(), "+=", ToExpr(context.expression()));

    public override AstNode VisitSubAssignment(KotlinParser.SubAssignmentContext context)
        => new AssignStmt(context.ID().GetText(), "-=", ToExpr(context.expression()));

    public override AstNode VisitMultAssignment(KotlinParser.MultAssignmentContext context)
        => new AssignStmt(context.ID().GetText(), "*=", ToExpr(context.expression()));

    public override AstNode VisitDivAssignment(KotlinParser.DivAssignmentContext context)
        => new AssignStmt(context.ID().GetText(), "/=", ToExpr(context.expression()));

    public override AstNode VisitModAssignment(KotlinParser.ModAssignmentContext context)
        => new AssignStmt(context.ID().GetText(), "%=", ToExpr(context.expression()));

    public override AstNode VisitPostIncrement(KotlinParser.PostIncrementContext context)
        => new IncDecStmt(context.ID().GetText(), true);

    public override AstNode VisitPostDecrement(KotlinParser.PostDecrementContext context)
        => new IncDecStmt(context.ID().GetText(), false);

    // xs[i] = v  ==>  ExprStmt(BinaryExpr("=", IndexExpr(xs, i), v))
    public override AstNode VisitIndexAssignment(KotlinParser.IndexAssignmentContext context)
    {
        var target = new IndexExpr(new IdentifierExpr(context.ID().GetText()), ToExpr(context.expression(0)));
        string op = context.GetChild(4).GetText();
        return new ExprStmt(new BinaryExpr(op, target, ToExpr(context.expression(1))));
    }

    // ---------- блоки и управляющие конструкции ----------

    public override AstNode VisitBlock(KotlinParser.BlockContext context)
        => new BlockStmt(BuildStatements(context.statements()));

    public override AstNode VisitControlStructureBody(KotlinParser.ControlStructureBodyContext context)
    {
        if (context.block() != null) return Visit(context.block());
        if (context.statement() != null) return Visit(context.statement());
        return null;
    }

    public override AstNode VisitIfExpression(KotlinParser.IfExpressionContext context)
    {
        var bodies = context.controlStructureBody();
        Stmt thenBranch = ToStmt(bodies[0]);
        Stmt elseBranch = bodies.Length > 1 ? ToStmt(bodies[1]) : null;
        return new IfStmt(ToExpr(context.expression()), thenBranch, elseBranch);
    }

    // for (i in a..b) / a until b / a downTo b / ... step s / xs.indices / любая коллекция
    public override AstNode VisitForStatement(KotlinParser.ForStatementContext context)
    {
        string v = context.ID().GetText();
        Stmt body = ToStmt(context.controlStructureBody());
        var iterable = context.expression();

        Expr step = null;
        KotlinParser.ExpressionContext rangeCtx = iterable;
        if (iterable is KotlinParser.StepExpContext stepCtx)
        {
            step = ToExpr(stepCtx.expression(1));
            rangeCtx = stepCtx.expression(0);
        }

        if (rangeCtx is KotlinParser.RangeExpContext range)
        {
            string op = range.GetChild(1).GetText();
            Expr from = ToExpr(range.expression(0));
            Expr to = ToExpr(range.expression(1));

            if (op == ".." && step == null) return new ForRangeStmt(v, from, to, body);

            var variable = new IdentifierExpr(v);
            var init = new VarDeclStmt(false, v, from, new TypeRef(DataType.Int, "Int"));

            if (op == "downTo")
            {
                Stmt update = step == null ? new IncDecStmt(v, false) : new AssignStmt(v, "-=", step);
                return new ForStmt(init, new BinaryExpr(">=", variable, to), update, body);
            }
            else
            {
                Stmt update = step == null ? new IncDecStmt(v, true) : new AssignStmt(v, "+=", step);
                string cmp = op == "until" ? "<" : "<=";
                return new ForStmt(init, new BinaryExpr(cmp, variable, to), update, body);
            }
        }

        if (iterable is KotlinParser.MemberAccessContext member && member.ID().GetText() == "indices")
        {
            Expr target = ToExpr(member.expression());
            Expr last = new BinaryExpr("-", new MemberExpr(target, "size"), One);
            return new ForRangeStmt(v, Zero, last, body);
        }

        return new ForEachStmt(v, null, ToExpr(iterable), body);
    }

    public override AstNode VisitWhileStatement(KotlinParser.WhileStatementContext context)
        => new WhileStmt(ToExpr(context.expression()), ToStmt(context.controlStructureBody()));

    public override AstNode VisitDoWhileStatement(KotlinParser.DoWhileStatementContext context)
        => new DoWhileStmt(ToStmt(context.controlStructureBody()), ToExpr(context.expression()));

    public override AstNode VisitFunctionDeclaration(KotlinParser.FunctionDeclarationContext context)
    {
        string name = context.ID().GetText();

        var parameters = new List<Parameter>();
        foreach (var param in context.parameter())
        {
            parameters.Add(new Parameter(
                param.ID().GetText(),
                ToTypeRef(param.kotlinType()),
                ToExpr(param.expression())));
        }

        var functionBody = context.functionBody();
        BlockStmt body = null;
        bool isExpressionBody = false;

        if (functionBody != null && functionBody.block() != null)
        {
            body = Visit(functionBody.block()) as BlockStmt;
        }
        else if (functionBody != null && functionBody.ASSIGNMENT() != null)
        {
            // fun f() = expr   ==>   { return expr; }
            body = new BlockStmt(new List<Stmt> { new ReturnStmt(ToExpr(functionBody.expression())) });
            isExpressionBody = true;
        }

        // Тип не указан: для блока это Unit, для "= expr" тип надо вывести (Unknown с пустым именем)
        TypeRef returnType = context.kotlinType() != null
            ? ToTypeRef(context.kotlinType())
            : (isExpressionBody ? NoType : TypeRef.Void);

        return new FunctionDecl(name, parameters, returnType, body, isExpressionBody);
    }

    // ---------- when ----------

    // when как инструкция -> цепочка if / else if / else
    public override AstNode VisitWhenExpression(KotlinParser.WhenExpressionContext context)
    {
        Expr subject = ToExpr(context.expression());
        Stmt elseBody = null;
        var branches = new List<(Expr Cond, Stmt Body)>();

        foreach (var entry in context.whenEntry())
        {
            Stmt body = ToStmt(entry.controlStructureBody());
            if (entry.ELSE() != null) elseBody = body;
            else branches.Add((BuildWhenCondition(subject, entry), body));
        }

        Stmt result = elseBody;
        for (int i = branches.Count - 1; i >= 0; i--)
        {
            result = new IfStmt(branches[i].Cond, branches[i].Body, result);
        }
        return result ?? new BlockStmt(new List<Stmt>());
    }

    // when как выражение -> вложенные тернарные операторы
    public override AstNode VisitWhenAsExpr(KotlinParser.WhenAsExprContext context)
    {
        var when = context.whenExpression();
        Expr subject = ToExpr(when.expression());
        Expr elseValue = null;
        var branches = new List<(Expr Cond, Expr Value)>();

        foreach (var entry in when.whenEntry())
        {
            Expr value = StmtToExpr(ToStmt(entry.controlStructureBody()));
            if (entry.ELSE() != null) elseValue = value;
            else branches.Add((BuildWhenCondition(subject, entry), value));
        }

        Expr result = elseValue ?? Zero;
        for (int i = branches.Count - 1; i >= 0; i--)
        {
            result = new TernaryExpr(branches[i].Cond, branches[i].Value, result);
        }
        return result;
    }

    private static Expr StmtToExpr(Stmt stmt)
    {
        if (stmt is ExprStmt e) return e.Expression;
        if (stmt is BlockStmt { Statements.Count: 1 } b && b.Statements[0] is ExprStmt single) return single.Expression;
        return new RawExpr("0 /* unsupported when branch */");
    }

    // Условия одной ветки: "1, 2 ->" => (s == 1 || s == 2); "in 1..5 ->" => диапазон; без subject — просто булево выражение
    private Expr BuildWhenCondition(Expr subject, KotlinParser.WhenEntryContext entry)
    {
        Expr result = null;

        foreach (var cond in entry.whenCondition())
        {
            Expr one = null;

            if (cond is KotlinParser.WhenInConditionContext inCond)
            {
                if (subject != null) one = MakeIn(subject, inCond.expression());
            }
            else if (cond is KotlinParser.WhenValueConditionContext valueCond)
            {
                Expr value = ToExpr(valueCond.expression());
                one = subject == null ? value : new BinaryExpr("==", subject, value);
            }

            if (one == null) continue;
            result = result == null ? one : new BinaryExpr("||", result, one);
        }

        return result ?? new LiteralExpr(LiteralKind.Bool, "true");
    }

    // left in right: диапазон -> два сравнения, иначе right.contains(left)
    private Expr MakeIn(Expr left, KotlinParser.ExpressionContext right)
    {
        if (right is KotlinParser.RangeExpContext range)
        {
            string op = range.GetChild(1).GetText();
            Expr a = ToExpr(range.expression(0));
            Expr b = ToExpr(range.expression(1));

            Expr check = op switch
            {
                "downTo" => new BinaryExpr("&&", new BinaryExpr("<=", left, a), new BinaryExpr(">=", left, b)),
                "until" => new BinaryExpr("&&", new BinaryExpr(">=", left, a), new BinaryExpr("<", left, b)),
                _ => new BinaryExpr("&&", new BinaryExpr(">=", left, a), new BinaryExpr("<=", left, b))
            };
            return new ParenExpr(check);
        }

        return new CallExpr(ToExpr(right), "contains", new List<Expr> { left });
    }

    // ---------- прыжки и исключения ----------

    public override AstNode VisitJumpExpression(KotlinParser.JumpExpressionContext context)
    {
        if (context.RETURN() != null) return new ReturnStmt(ToExpr(context.expression()));
        if (context.CONTINUE() != null) return new ContinueStmt();
        if (context.BREAK() != null) return new BreakStmt();
        if (context.THROW() != null) return new ThrowStmt(ToExpr(context.expression()));
        return null;
    }

    public override AstNode VisitTryExpression(KotlinParser.TryExpressionContext context)
    {
        var catches = new List<CatchClause>();

        foreach (var catchBlock in context.catchBlock())
        {
            string paramName = "e";
            string exceptionType = null;

            var ids = catchBlock.ID();
            if (ids.Length == 1)
            {
                paramName = ids[0].GetText();              // catch (e) — тип не указан
            }
            else if (ids.Length >= 2)
            {
                paramName = ids[0].GetText();
                exceptionType = ids[1].GetText();
            }

            catches.Add(new CatchClause(paramName, exceptionType, Visit(catchBlock.block()) as BlockStmt));
        }

        BlockStmt finallyBlock = context.finallyBlock() != null
            ? Visit(context.finallyBlock().block()) as BlockStmt
            : null;

        return new TryStmt(Visit(context.block()) as BlockStmt, catches, finallyBlock);
    }

    // ---------- то, что не переводится ----------

    public override AstNode VisitUnparsedStatement(KotlinParser.UnparsedStatementContext context)
    {
        string raw = AntlrText.Raw(context);
        return string.IsNullOrWhiteSpace(raw)
            ? null
            : new UnsupportedStmt("Unsupported statement construction", raw);
    }

    // ---------- вызовы, члены, индексы, лямбды ----------

    // callee(args): callee — идентификатор (свободная функция) или obj.name (метод)
    public override AstNode VisitFunctionCall(KotlinParser.FunctionCallContext context)
    {
        var all = context.expression();
        Expr callee = ToExpr(all[0]);
        var args = ToExprs(all.Skip(1));

        Expr result = callee switch
        {
            IdentifierExpr id => new CallExpr(null, id.Name, args),
            MemberExpr m => new CallExpr(m.Target, m.Name, args),
            _ => new RawExpr(AntlrText.Raw(context))
        };
        return result;
    }

    // name<T>(args)
    public override AstNode VisitGenericCall(KotlinParser.GenericCallContext context)
    {
        var typeArgs = context.typeArguments().kotlinType().Select(ToTypeRef).ToList();
        return new CallExpr(null, context.ID().GetText(), ToExprs(context.expression()), typeArgs);
    }

    // callee { lambda }  — лямбда становится последним аргументом
    public override AstNode VisitTrailingLambda(KotlinParser.TrailingLambdaContext context)
    {
        Expr callee = ToExpr(context.expression());
        LambdaExpr lambda = BuildLambda(context.lambdaLiteral());

        Expr result = callee switch
        {
            CallExpr c => c with { Args = c.Args.Append(lambda).ToList() },
            IdentifierExpr id => new CallExpr(null, id.Name, new List<Expr> { lambda }),
            MemberExpr m => new CallExpr(m.Target, m.Name, new List<Expr> { lambda }),
            _ => new RawExpr(AntlrText.Raw(context))
        };
        return result;
    }

    public override AstNode VisitMemberAccess(KotlinParser.MemberAccessContext context)
        => new MemberExpr(ToExpr(context.expression()), context.ID().GetText());

    public override AstNode VisitIndex(KotlinParser.IndexContext context)
        => new IndexExpr(ToExpr(context.expression(0)), ToExpr(context.expression(1)));

    // x!! — просто отбрасываем
    public override AstNode VisitNotNullAssert(KotlinParser.NotNullAssertContext context)
        => ToExpr(context.expression());

    public override AstNode VisitLambdaExp(KotlinParser.LambdaExpContext context)
        => BuildLambda(context.lambdaLiteral());

    private LambdaExpr BuildLambda(KotlinParser.LambdaLiteralContext ctx)
    {
        var parameters = new List<Parameter>();
        if (ctx.lambdaParams() != null)
        {
            foreach (var p in ctx.lambdaParams().lambdaParam())
            {
                parameters.Add(new Parameter(p.ID().GetText(), ToTypeRef(p.kotlinType()) ?? NoType));
            }
        }
        return new LambdaExpr(parameters, new BlockStmt(BuildStatements(ctx.statements())));
    }

    // if как выражение
    public override AstNode VisitIfExp(KotlinParser.IfExpContext context)
    {
        var e = context.expression();
        return new TernaryExpr(ToExpr(e[0]), ToExpr(e[1]), ToExpr(e[2]));
    }

    // Диапазоны вне for / in / when не переводим — отдаём как есть
    public override AstNode VisitRangeExp(KotlinParser.RangeExpContext context)
        => new RawExpr(AntlrText.Raw(context));

    public override AstNode VisitStepExp(KotlinParser.StepExpContext context)
        => new RawExpr(AntlrText.Raw(context));

    public override AstNode VisitInExp(KotlinParser.InExpContext context)
        => MakeIn(ToExpr(context.expression(0)), context.expression(1));

    public override AstNode VisitNotInExp(KotlinParser.NotInExpContext context)
        => new UnaryExpr("!", new ParenExpr(MakeIn(ToExpr(context.expression(0)), context.expression(1))));

    // ---------- выражения ----------

    public override AstNode VisitIdentifier(KotlinParser.IdentifierContext context)
    {
        string name = context.ID().GetText();
        if (name == "true" || name == "false") return new LiteralExpr(LiteralKind.Bool, name);
        return new IdentifierExpr(name);
    }

    public override AstNode VisitIntLiteral(KotlinParser.IntLiteralContext context)
        => new LiteralExpr(LiteralKind.Int, context.INT().GetText());

    public override AstNode VisitLongLiteral(KotlinParser.LongLiteralContext context)
        => new LiteralExpr(LiteralKind.Int, context.LONG().GetText());

    public override AstNode VisitDoubleLiteral(KotlinParser.DoubleLiteralContext context)
        => new LiteralExpr(LiteralKind.Double, context.DOUBLE().GetText());

    public override AstNode VisitCharLiteral(KotlinParser.CharLiteralContext context)
        => new LiteralExpr(LiteralKind.Char, context.CHAR_LITERAL().GetText());

    public override AstNode VisitStringLiteral(KotlinParser.StringLiteralContext context)
        => BuildString(context.STRING_LITERAL().GetText());

    public override AstNode VisitParens(KotlinParser.ParensContext context)
        => new ParenExpr(ToExpr(context.expression()));

    public override AstNode VisitUnaryMinus(KotlinParser.UnaryMinusContext context)
        => new UnaryExpr("-", ToExpr(context.expression()));

    public override AstNode VisitUnaryNot(KotlinParser.UnaryNotContext context)
        => new UnaryExpr("!", ToExpr(context.expression()));

    public override AstNode VisitMulDiv(KotlinParser.MulDivContext context)
        => new BinaryExpr(context.GetChild(1).GetText(), ToExpr(context.expression(0)), ToExpr(context.expression(1)));

    public override AstNode VisitAddSub(KotlinParser.AddSubContext context)
        => new BinaryExpr(context.GetChild(1).GetText(), ToExpr(context.expression(0)), ToExpr(context.expression(1)));

    public override AstNode VisitComparison(KotlinParser.ComparisonContext context)
        => new BinaryExpr(context.GetChild(1).GetText(), ToExpr(context.expression(0)), ToExpr(context.expression(1)));

    // В грамматике есть две пары меток для && и || — обрабатываем обе одинаково
    public override AstNode VisitAndOp(KotlinParser.AndOpContext ctx)
        => new BinaryExpr("&&", ToExpr(ctx.expression(0)), ToExpr(ctx.expression(1)));

    public override AstNode VisitOrOp(KotlinParser.OrOpContext ctx)
        => new BinaryExpr("||", ToExpr(ctx.expression(0)), ToExpr(ctx.expression(1)));

    public override AstNode VisitAndLogical(KotlinParser.AndLogicalContext ctx)
        => new BinaryExpr("&&", ToExpr(ctx.expression(0)), ToExpr(ctx.expression(1)));

    public override AstNode VisitOrLogical(KotlinParser.OrLogicalContext ctx)
        => new BinaryExpr("||", ToExpr(ctx.expression(0)), ToExpr(ctx.expression(1)));

    // ---------- строковые шаблоны ----------

    /// <summary>
    /// "text $name ${expr} text" -> TemplateExpr. Если подстановок нет — обычный LiteralExpr.
    /// Выражение внутри ${...} разбирается отдельным парсером (тем же KotlinParser.expression).
    /// </summary>
    private Expr BuildString(string literal)
    {
        string inner = literal.Substring(1, literal.Length - 2);
        var parts = new List<Expr>();
        var text = new StringBuilder();

        void FlushText()
        {
            if (text.Length == 0) return;
            parts.Add(new LiteralExpr(LiteralKind.String, "\"" + text + "\""));
            text.Clear();
        }

        for (int i = 0; i < inner.Length; i++)
        {
            char c = inner[i];

            if (c == '\\' && i + 1 < inner.Length)
            {
                // \$ в Kotlin — просто знак доллара; остальные escape-последовательности оставляем как есть
                if (inner[i + 1] == '$') text.Append('$');
                else text.Append(c).Append(inner[i + 1]);
                i++;
                continue;
            }

            if (c == '$' && i + 1 < inner.Length)
            {
                char next = inner[i + 1];

                if (next == '{')
                {
                    int depth = 1;
                    int j = i + 2;
                    while (j < inner.Length && depth > 0)
                    {
                        if (inner[j] == '{') depth++;
                        else if (inner[j] == '}') depth--;
                        j++;
                    }

                    if (depth == 0)
                    {
                        string code = inner.Substring(i + 2, j - i - 3);
                        FlushText();
                        parts.Add(ParseSnippet(code));
                        i = j - 1;
                        continue;
                    }
                }
                else if (char.IsLetter(next) || next == '_')
                {
                    int j = i + 1;
                    while (j < inner.Length && (char.IsLetterOrDigit(inner[j]) || inner[j] == '_')) j++;

                    FlushText();
                    parts.Add(new IdentifierExpr(inner.Substring(i + 1, j - i - 1)));
                    i = j - 1;
                    continue;
                }
            }

            text.Append(c);
        }
        FlushText();

        if (parts.Count == 0) return new LiteralExpr(LiteralKind.String, literal);
        if (parts.Count == 1 && parts[0] is LiteralExpr only) return only;
        return new TemplateExpr(parts);
    }

    private Expr ParseSnippet(string code)
    {
        var lexer = new KotlinLexer(new AntlrInputStream(code));
        var parser = new KotlinParser(new CommonTokenStream(lexer));
        return ToExpr(parser.expression()) ?? new RawExpr(code);
    }
}