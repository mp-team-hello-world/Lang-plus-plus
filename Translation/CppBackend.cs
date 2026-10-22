using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Kotlin_plus_plus;

/// <summary>IR -> C++ (C++17).</summary>
public sealed class CppBackend : ITargetBackend
{
    public string Generate(ProgramNode program) => new Generator().Generate(program);

    // ======================================================================================
    //  Вспомогательные шаблоны C++. Попадают в вывод только если реально использованы.
    //  Needs — от каких хелперов зависит (они выводятся раньше).
    // ======================================================================================
    private static readonly Dictionary<string, (string Code, string[] Needs)> Helpers = new()
    {
        ["kt_io"] = (@"static const bool kt_io_init = (cout << boolalpha, true);", Array.Empty<string>()),

        ["kt_str"] = (@"template <class T> string kt_str(const T& v) { ostringstream o; o << boolalpha << v; return o.str(); }",
            Array.Empty<string>()),

        ["kt_readLine"] = (@"inline string kt_readLine() { string s; getline(cin, s); return s; }", Array.Empty<string>()),

        ["kt_contains"] = (
@"template <class C, class T> bool kt_contains(const C& c, const T& x) { return find(c.begin(), c.end(), x) != c.end(); }
inline bool kt_contains(const string& s, const string& x) { return s.find(x) != string::npos; }
inline bool kt_contains(const string& s, const char* x) { return s.find(x) != string::npos; }
inline bool kt_contains(const string& s, char x) { return s.find(x) != string::npos; }", Array.Empty<string>()),

        ["kt_indexOf"] = (@"template <class C, class T> int kt_indexOf(const C& c, const T& x) { auto it = find(c.begin(), c.end(), x); return it == c.end() ? -1 : (int)(it - c.begin()); }",
            Array.Empty<string>()),

        ["kt_remove"] = (@"template <class C, class T> bool kt_remove(C& c, const T& x) { auto it = find(c.begin(), c.end(), x); if (it == c.end()) return false; c.erase(it); return true; }",
            Array.Empty<string>()),

        ["kt_addAll"] = (@"template <class C, class D> void kt_addAll(C& c, const D& d) { c.insert(c.end(), d.begin(), d.end()); }",
            Array.Empty<string>()),

        ["kt_sum"] = (@"template <class C> auto kt_sum(const C& c) { typename C::value_type s{}; for (const auto& e : c) s += e; return s; }",
            Array.Empty<string>()),

        ["kt_max"] = (@"template <class C> auto kt_max(const C& c) { return *max_element(c.begin(), c.end()); }", Array.Empty<string>()),

        ["kt_min"] = (@"template <class C> auto kt_min(const C& c) { return *min_element(c.begin(), c.end()); }", Array.Empty<string>()),

        ["kt_average"] = (@"template <class C> double kt_average(const C& c) { double s = 0; for (const auto& e : c) s += e; return c.empty() ? 0.0 : s / c.size(); }",
            Array.Empty<string>()),

        ["kt_sorted"] = (@"template <class C> C kt_sorted(C c) { sort(c.begin(), c.end()); return c; }", Array.Empty<string>()),

        ["kt_sortedDesc"] = (@"template <class C> C kt_sortedDesc(C c) { sort(c.begin(), c.end(), greater<>()); return c; }", Array.Empty<string>()),

        ["kt_reversed"] = (@"template <class C> C kt_reversed(C c) { reverse(c.begin(), c.end()); return c; }", Array.Empty<string>()),

        ["kt_join"] = (@"template <class C> string kt_join(const C& c, const string& sep = "", "") { string r; bool first = true; for (const auto& e : c) { if (!first) r += sep; first = false; r += kt_str(e); } return r; }",
            new[] { "kt_str" }),

        ["kt_forEach"] = (@"template <class C, class F> void kt_forEach(const C& c, F f) { for (const auto& e : c) f(e); }", Array.Empty<string>()),

        ["kt_map"] = (@"template <class C, class F> auto kt_map(const C& c, F f) { vector<decay_t<decltype(f(*c.begin()))>> r; for (const auto& e : c) r.push_back(f(e)); return r; }",
            Array.Empty<string>()),

        ["kt_filter"] = (@"template <class C, class F> C kt_filter(const C& c, F f) { C r; for (const auto& e : c) if (f(e)) r.push_back(e); return r; }",
            Array.Empty<string>()),

        ["kt_any"] = (@"template <class C, class F> bool kt_any(const C& c, F f) { for (const auto& e : c) if (f(e)) return true; return false; }",
            Array.Empty<string>()),

        ["kt_all"] = (@"template <class C, class F> bool kt_all(const C& c, F f) { for (const auto& e : c) if (!f(e)) return false; return true; }",
            Array.Empty<string>()),

        ["kt_none"] = (@"template <class C, class F> bool kt_none(const C& c, F f) { for (const auto& e : c) if (f(e)) return false; return true; }",
            Array.Empty<string>()),

        ["kt_count"] = (@"template <class C, class F> int kt_count(const C& c, F f) { int n = 0; for (const auto& e : c) if (f(e)) n++; return n; }",
            Array.Empty<string>()),

        ["kt_sumOf"] = (@"template <class C, class F> auto kt_sumOf(const C& c, F f) { decay_t<decltype(f(*c.begin()))> s{}; for (const auto& e : c) s += f(e); return s; }",
            Array.Empty<string>()),

        ["kt_toInt"] = (
@"inline int kt_toInt(const string& s) { return stoi(s); }
template <class T> int kt_toInt(const T& v) { return static_cast<int>(v); }", Array.Empty<string>()),

        ["kt_toLong"] = (
@"inline long kt_toLong(const string& s) { return stol(s); }
template <class T> long kt_toLong(const T& v) { return static_cast<long>(v); }", Array.Empty<string>()),

        ["kt_toDouble"] = (
@"inline double kt_toDouble(const string& s) { return stod(s); }
template <class T> double kt_toDouble(const T& v) { return static_cast<double>(v); }", Array.Empty<string>()),

        ["kt_upper"] = (@"inline string kt_upper(string s) { for (auto& c : s) c = (char)toupper((unsigned char)c); return s; }", Array.Empty<string>()),

        ["kt_lower"] = (@"inline string kt_lower(string s) { for (auto& c : s) c = (char)tolower((unsigned char)c); return s; }", Array.Empty<string>()),

        ["kt_startsWith"] = (@"inline bool kt_startsWith(const string& s, const string& p) { return s.rfind(p, 0) == 0; }", Array.Empty<string>()),

        ["kt_endsWith"] = (@"inline bool kt_endsWith(const string& s, const string& p) { return s.size() >= p.size() && s.compare(s.size() - p.size(), p.size(), p) == 0; }",
            Array.Empty<string>()),

        ["kt_split"] = (@"inline vector<string> kt_split(const string& s, const string& sep) { if (sep.empty()) return {s}; vector<string> r; size_t p = 0, q; while ((q = s.find(sep, p)) != string::npos) { r.push_back(s.substr(p, q - p)); p = q + sep.size(); } r.push_back(s.substr(p)); return r; }",
            Array.Empty<string>()),
    };

    // Лямбды с неявным параметром it
    private static readonly HashSet<string> OneArgLambdaMethods = new()
    {
        "forEach", "map", "filter", "any", "all", "none", "count", "sumOf"
    };

    private static readonly HashSet<string> MutableCtors = new()
    {
        "mutableListOf", "arrayListOf", "arrayOf", "intArrayOf", "longArrayOf", "doubleArrayOf",
        "booleanArrayOf", "charArrayOf", "IntArray", "LongArray", "DoubleArray", "BooleanArray",
        "CharArray", "ArrayList"
    };

    private static readonly HashSet<string> ListCtors = new(MutableCtors) { "listOf", "emptyList" };

    private static readonly Dictionary<string, string> TypedArrayCtors = new()
    {
        ["intArrayOf"] = "int", ["longArrayOf"] = "long", ["doubleArrayOf"] = "double",
        ["booleanArrayOf"] = "bool", ["charArrayOf"] = "char",
        ["IntArray"] = "int", ["LongArray"] = "long", ["DoubleArray"] = "double",
        ["BooleanArray"] = "bool", ["CharArray"] = "char"
    };

    private sealed class Generator : IStmtVisitor, IExprVisitor<string>
    {
        private CodeWriter _w = new();
        private readonly List<string> _prototypes = new();
        private readonly List<string> _usedHelpers = new();
        private readonly HashSet<string> _usedSet = new();

        // имя переменной -> C++-тип (где удалось узнать); используется для вывода типа элементов списка
        private readonly Dictionary<string, string> _varTypes = new();

        // тип объявляемой переменной, пока рендерится её инициализатор (нужен для listOf() без аргументов)
        private TypeRef _expectedType;

        public string Generate(ProgramNode program)
        {
            EmitProgram(program);
            return BuildResult();
        }

        private void Need(string helper)
        {
            if (_usedSet.Contains(helper)) return;
            foreach (var dep in Helpers[helper].Needs) Need(dep);
            _usedSet.Add(helper);
            _usedHelpers.Add(helper);
        }

        // Заголовок -> признак его использования в сгенерированном коде.
        // Порядок списка = порядок #include в выводе.
        private static readonly (string Header, Regex Usage)[] HeaderRules =
        {
            ("iostream",   new Regex(@"\b(cout|cin|cerr|endl|boolalpha)\b")),
            ("string",     new Regex(@"\b(string|getline|stoi|stol|stod|to_string)\b")),
            ("vector",     new Regex(@"\bvector\b")),
            ("algorithm",  new Regex(@"\b(sort|reverse|find|max_element|min_element|max|min)\s*\(")),
            ("numeric",    new Regex(@"\b(accumulate|iota)\b")),
            ("functional", new Regex(@"\bgreater\s*<")),
            ("sstream",    new Regex(@"\b(ostringstream|istringstream|stringstream)\b")),
            ("cmath",      new Regex(@"\b(sqrt|pow|abs|floor|ceil|round|fabs|sin|cos|tan|log|log10|exp|hypot|cbrt)\s*\(")),
            ("cctype",     new Regex(@"\b(toupper|tolower|isalpha|isdigit|isspace)\s*\(")),
            ("exception",  new Regex(@"\bexception\b")),
            ("stdexcept",  new Regex(@"\b(runtime_error|invalid_argument|out_of_range|logic_error|domain_error)\b")),
            ("type_traits", new Regex(@"\bdecay_t\b")),
        };

        private static readonly Regex StringLiteral = new(@"""(\\.|[^""\\])*""|'(\\.|[^'\\])*'", RegexOptions.Compiled);
        private static readonly Regex Comments = new(@"/\*.*?\*/|//[^\n]*", RegexOptions.Compiled | RegexOptions.Singleline);

        // Хелперы в таблице записаны компактно (по строке на функцию). Здесь раскладываем их
        // в тот же стиль, что и остальной вывод: скобки на отдельных строках, по оператору на строку.
        private static string Pretty(string code)
        {
            var lines = new List<string>();
            var cur = new StringBuilder();
            var blocks = new Stack<bool>();   // true — фигурная скобка блока, false — инициализатор {...}
            int indent = 0, paren = 0;

            void Flush()
            {
                string text = cur.ToString().Trim();
                if (text.Length > 0) lines.Add(new string(' ', indent * 4) + text);
                cur.Clear();
            }

            char LastNonSpace()
            {
                for (int i = cur.Length - 1; i >= 0; i--)
                    if (!char.IsWhiteSpace(cur[i])) return cur[i];
                return ' ';
            }

            foreach (char c in code)
            {
                if (c == '\n') { Flush(); continue; }

                if (c == '(') paren++;
                else if (c == ')') paren--;

                if (c == '{')
                {
                    // блок начинается после ')' на нулевой глубине скобок; остальное — инициализатор вроде s{} или {s}
                    bool isBlock = paren == 0 && LastNonSpace() == ')';
                    blocks.Push(isBlock);
                    if (isBlock)
                    {
                        Flush();
                        lines.Add(new string(' ', indent * 4) + "{");
                        indent++;
                    }
                    else cur.Append(c);
                    continue;
                }

                if (c == '}')
                {
                    if (blocks.Count > 0 && blocks.Pop())
                    {
                        Flush();
                        indent--;
                        lines.Add(new string(' ', indent * 4) + "}");
                    }
                    else cur.Append(c);
                    continue;
                }

                cur.Append(c);
                if (c == ';' && paren == 0 && (blocks.Count == 0 || blocks.Peek())) Flush();
            }
            Flush();

            return string.Join("\n", lines);
        }

        private string BuildResult()
        {
            var body = new List<string>();

            foreach (var name in _usedHelpers)
            {
                body.Add(Pretty(Helpers[name].Code));
                body.Add("");
            }

            if (_prototypes.Count > 0)
            {
                body.AddRange(_prototypes);
                body.Add("");
            }
            body.AddRange(_w.Lines);

            // Заголовки определяем по тексту результата (без строковых литералов и комментариев):
            // так не нужно вести отдельные флаги "что использовано".
            string code = string.Join("\n", body);
            string scanned = Comments.Replace(StringLiteral.Replace(code, "\"\""), "");

            var result = new List<string>();
            foreach (var (header, usage) in HeaderRules)
            {
                if (usage.IsMatch(scanned)) result.Add($"#include <{header}>");
            }
            if (result.Count > 0) result.Add("");

            result.Add("using namespace std;");
            result.Add("");
            result.AddRange(body);

            return string.Join("\n", result);
        }

        private void Emit(Stmt stmt) => stmt?.Accept(this);

        private string Render(Expr expr) => expr == null ? string.Empty : expr.Accept(this);

        // ---------- программа: main и прототипы ----------

        private void EmitProgram(ProgramNode program)
        {
            var statements = program.Statements;

            if (statements.Count == 0)
            {
                EmitEmptyMain();
                return;
            }

            bool hasMain = statements.OfType<FunctionDecl>().Any(f => f.Name == "main");

            if (hasMain)
            {
                // main уже есть — выводим всё в исходном порядке
                foreach (var stmt in statements) Emit(stmt);
                return;
            }

            // main нет: сначала функции, потом всё остальное заворачиваем в int main()
            foreach (var function in statements.OfType<FunctionDecl>()) Emit(function);

            _w.Line("int main()");
            _w.Line("{");
            _w.Indent();
            foreach (var stmt in statements)
            {
                if (stmt is not FunctionDecl) Emit(stmt);
            }
            _w.Line("return 0;");
            _w.Dedent();
            _w.Line("}");
        }

        private void EmitEmptyMain()
        {
            _w.Line("int main()");
            _w.Line("{");
            _w.Indent();
            _w.Line("return 0;");
            _w.Dedent();
            _w.Line("}");
        }

        // Тело if/for/while: блок — как есть, одиночная инструкция — с отступом
        private void EmitBody(Stmt body)
        {
            if (body == null) return;

            if (body is BlockStmt)
            {
                Emit(body);
            }
            else
            {
                _w.Indent();
                Emit(body);
                _w.Dedent();
            }
        }

        // ---------- типы ----------

        private static string ToCppType(TypeRef type)
        {
            if (type == null) return "auto";

            return type.Kind switch
            {
                DataType.Int => "int",
                DataType.Bool => "bool",
                DataType.Long => "long",
                DataType.Void => "void",
                DataType.Char => "char",
                DataType.Float => "float",
                DataType.Double => "double",
                DataType.String => "string",
                DataType.List => $"vector<{ElementType(type)}>",
                _ => "auto"
            };
        }

        private static string ElementType(TypeRef list)
        {
            if (list.Args == null || list.Args.Count == 0) return "int";
            string element = ToCppType(list.Args[0]);
            return element == "auto" ? "int" : element;
        }

        private static bool IsMutableListType(TypeRef type)
            => type is { Kind: DataType.List } && type.SourceName != "List" && type.SourceName != "Collection";

        // Тип параметра функции: списки и строки передаём по ссылке
        private static string ParamType(TypeRef type)
        {
            string cpp = ToCppType(type);
            if (type == null) return cpp;

            if (type.Kind == DataType.List) return IsMutableListType(type) ? $"{cpp}&" : $"const {cpp}&";
            if (type.Kind == DataType.String) return "const string&";
            return cpp;
        }

        // ---------- выражения ----------

        public string VisitLiteral(LiteralExpr node) => node.Text;

        public string VisitIdentifier(IdentifierExpr node) => node.Name;

        public string VisitParen(ParenExpr node) => $"({Render(node.Inner)})";

        public string VisitUnary(UnaryExpr node) => $"{node.Op}{Render(node.Operand)}";

        public string VisitBinary(BinaryExpr node) => $"{Render(node.Left)} {node.Op} {Render(node.Right)}";

        public string VisitRaw(RawExpr node) => node.SourceText;

        public string VisitTernary(TernaryExpr node)
            => $"({Render(node.Condition)} ? {Render(node.Then)} : {Render(node.Else)})";

        public string VisitIndex(IndexExpr node) => $"{Render(node.Target)}[{Render(node.Index)}]";

        public string VisitMember(MemberExpr node)
        {
            string target = Render(node.Target);
            return node.Name switch
            {
                "size" or "length" => $"((int){target}.size())",
                "lastIndex" => $"((int){target}.size() - 1)",
                "message" => $"string({target}.what())",
                _ => $"{target}.{node.Name}"
            };
        }

        // "a $x b" как значение: string("a ") + kt_str(x) + " b"
        public string VisitTemplate(TemplateExpr node)
        {
            var pieces = node.Parts
                .Select(p => p is LiteralExpr literal ? literal.Text : StrOf(p))
                .ToList();

            if (node.Parts[0] is LiteralExpr) pieces[0] = $"string({pieces[0]})";
            return "(" + string.Join(" + ", pieces) + ")";
        }

        private string StrOf(Expr expr)
        {
            Need("kt_str");
            return $"kt_str({Render(expr)})";
        }

        public string VisitLambda(LambdaExpr node) => RenderLambda(node, returnsValue: true, implicitIt: false);

        // Лямбда одной строкой: [&](auto it) { stmt; stmt; return expr; }
        private string RenderLambda(LambdaExpr lambda, bool returnsValue, bool implicitIt)
        {
            var saved = _w;
            _w = new CodeWriter("");

            var statements = lambda.Body.Statements;
            for (int i = 0; i < statements.Count; i++)
            {
                if (returnsValue && i == statements.Count - 1 && statements[i] is ExprStmt last)
                    _w.Line($"return {Render(last.Expression)};");
                else
                    Emit(statements[i]);
            }

            string body = string.Join(" ", _w.Lines.Select(l => l.Trim()));
            _w = saved;

            string parameters = lambda.Params.Count > 0
                ? string.Join(", ", lambda.Params.Select(p => $"{LambdaParamType(p.Type)} {p.Name}"))
                : (implicitIt ? "const auto& it" : "");

            return $"[&]({parameters}) {{ {body} }}";
        }

        private static string LambdaParamType(TypeRef type)
        {
            string cpp = ToCppType(type);
            return cpp == "auto" ? "const auto&" : cpp;
        }

        private string Arg(Expr arg, bool unitLambda = false, bool implicitIt = false)
            => arg is LambdaExpr lambda
                ? RenderLambda(lambda, returnsValue: !unitLambda, implicitIt)
                : Render(arg);

        // ---------- вызовы ----------

        public string VisitCall(CallExpr node)
        {
            var expected = _expectedType;
            _expectedType = null;

            return node.Receiver == null ? RenderFreeCall(node, expected) : RenderMethodCall(node);
        }

        private string RenderFreeCall(CallExpr node, TypeRef expected)
        {
            var args = node.Args;
            string All() => string.Join(", ", args.Select(a => Arg(a)));

            switch (node.Name)
            {
                case "println": return Print(args, newline: true);
                case "print": return Print(args, newline: false);

                case "readLine":
                case "readln":
                    Need("kt_readLine");
                    return "kt_readLine()";

                case "maxOf":
                case "minOf":
                {
                    string fn = node.Name == "maxOf" ? "max" : "min";
                    return args.Count == 2 ? $"{fn}({All()})" : $"{fn}({{{All()}}})";
                }
            }

            if (ListCtors.Contains(node.Name)) return RenderListCtor(node, expected);

            if (node.Name.EndsWith("Exception") || node.Name.EndsWith("Error"))
                return args.Count == 0 ? "runtime_error(\"\")" : $"runtime_error({All()})";

            return $"{node.Name}({All()})";
        }

        // println(x) -> cout << x << endl ; шаблоны печатаются по кускам, без склейки строк
        private string Print(IReadOnlyList<Expr> args, bool newline)
        {
            Need("kt_io");
            var sb = new StringBuilder("cout");

            if (args.Count > 0)
            {
                if (args[0] is TemplateExpr template)
                {
                    foreach (var part in template.Parts) sb.Append(" << ").Append(StreamOperand(part));
                }
                else
                {
                    sb.Append(" << ").Append(StreamOperand(args[0]));
                }
            }

            if (newline) sb.Append(" << endl");
            return sb.ToString();
        }

        // Операнд для <<: всё, что приоритетом ниже <<, берём в скобки
        private string StreamOperand(Expr expr)
        {
            string text = Render(expr);
            bool arithmetic = expr is BinaryExpr { Op: "+" or "-" or "*" or "/" or "%" };
            return expr is BinaryExpr && !arithmetic ? $"({text})" : text;
        }

        // listOf / mutableListOf / arrayOf / IntArray(n) / emptyList ...
        private string RenderListCtor(CallExpr node, TypeRef expected)
        {
            var args = node.Args;

            // Sized-конструкторы: IntArray(n), ArrayList<Int>()
            bool sized = node.Name is "IntArray" or "LongArray" or "DoubleArray" or "BooleanArray" or "CharArray" or "ArrayList";

            string element = null;
            if (TypedArrayCtors.TryGetValue(node.Name, out var fixedElement)) element = fixedElement;
            else if (node.TypeArgs is { Count: > 0 }) element = ToCppType(node.TypeArgs[0]);
            else if (expected is { Kind: DataType.List, Args.Count: > 0 }) element = ToCppType(expected.Args[0]);
            else if (!sized) element = InferElementType(args);

            if (element == "auto") element = null;

            if (sized)
            {
                string size = args.Count > 0 ? Render(args[0]) : "";
                return $"vector<{element ?? "int"}>({size})";
            }

            // вложенные литералы получают ожидаемый тип элемента
            TypeRef elementType = expected is { Kind: DataType.List, Args.Count: > 0 } ? expected.Args[0] : null;
            var rendered = new List<string>();
            foreach (var a in args)
            {
                _expectedType = elementType;
                rendered.Add(Render(a));
                _expectedType = null;
            }
            string items = string.Join(", ", rendered);

            if (element == null) return args.Count == 0 ? "vector<int>{}" : $"vector{{{items}}}"; // CTAD (C++17)
            return $"vector<{element}>{{{items}}}";
        }

        private string InferElementType(IReadOnlyList<Expr> args)
        {
            var types = args.Select(InferType).Where(t => t != null).ToList();
            if (types.Count == 0) return null;
            return types.Contains("double") ? "double" : types[0];
        }

        // Лёгкий вывод типа выражения — достаточно, чтобы типизировать литералы списков
        private string InferType(Expr expr)
        {
            switch (expr)
            {
                case LiteralExpr l:
                    return l.Kind switch
                    {
                        LiteralKind.Int => l.Text.EndsWith("L") ? "long" : "int",
                        LiteralKind.Double => l.Text.EndsWith("f") || l.Text.EndsWith("F") ? "float" : "double",
                        LiteralKind.String => "string",
                        LiteralKind.Bool => "bool",
                        LiteralKind.Char => "char",
                        _ => null
                    };
                case TemplateExpr: return "string";
                case ParenExpr p: return InferType(p.Inner);
                case UnaryExpr u: return u.Op == "!" ? "bool" : InferType(u.Operand);
                case BinaryExpr b:
                    if (b.Op is "<" or ">" or "<=" or ">=" or "==" or "!=" or "&&" or "||") return "bool";
                    var left = InferType(b.Left);
                    var right = InferType(b.Right);
                    return left == "double" || right == "double" ? "double" : left ?? right;
                case IdentifierExpr id:
                    return _varTypes.TryGetValue(id.Name, out var t) ? t : null;
                case CallExpr c when c.Receiver == null && ListCtors.Contains(c.Name):
                    var inner = TypedArrayCtors.TryGetValue(c.Name, out var fixedType) ? fixedType : InferElementType(c.Args);
                    return inner == null ? null : $"vector<{inner}>";
                default:
                    return null;
            }
        }

        private string RenderMethodCall(CallExpr node)
        {
            string r = Render(node.Receiver);
            var a = node.Args;
            string A(int i) => Render(a[i]);

            switch (node.Name)
            {
                // ----- изменяемые списки -----
                case "add" when a.Count == 1: return $"{r}.push_back({A(0)})";
                case "add" when a.Count == 2: return $"{r}.insert({r}.begin() + {A(0)}, {A(1)})";
                case "addAll": Need("kt_addAll"); return $"kt_addAll({r}, {A(0)})";
                case "removeAt": return $"{r}.erase({r}.begin() + {A(0)})";
                case "removeLast": return $"{r}.pop_back()";
                case "remove": Need("kt_remove"); return $"kt_remove({r}, {A(0)})";
                case "clear": return $"{r}.clear()";
                case "set" when a.Count == 2: return $"{r}[{A(0)}] = {A(1)}";
                case "sort": return $"sort({r}.begin(), {r}.end())";
                case "sortDescending": return $"sort({r}.begin(), {r}.end(), greater<>())";
                case "reverse": return $"reverse({r}.begin(), {r}.end())";

                // ----- запросы -----
                case "get" when a.Count == 1: return $"{r}[{A(0)}]";
                case "isEmpty": return $"{r}.empty()";
                case "isNotEmpty": return $"(!{r}.empty())";
                case "first": return $"{r}.front()";
                case "last": return $"{r}.back()";
                case "contains": Need("kt_contains"); return $"kt_contains({r}, {A(0)})";
                case "indexOf": Need("kt_indexOf"); return $"kt_indexOf({r}, {A(0)})";

                case "sorted": Need("kt_sorted"); return $"kt_sorted({r})";
                case "sortedDescending": Need("kt_sortedDesc"); return $"kt_sortedDesc({r})";
                case "reversed": Need("kt_reversed"); return $"kt_reversed({r})";
                case "sum": Need("kt_sum"); return $"kt_sum({r})";
                case "max":
                case "maxOrNull": Need("kt_max"); return $"kt_max({r})";
                case "min":
                case "minOrNull": Need("kt_min"); return $"kt_min({r})";
                case "average": Need("kt_average"); return $"kt_average({r})";
                case "toList":
                case "toMutableList": return r;

                case "joinToString":
                    Need("kt_join");
                    return a.Count == 0 ? $"kt_join({r})" : $"kt_join({r}, {A(0)})";

                // ----- функции высшего порядка -----
                case "forEach" when a.Count == 1:
                    Need("kt_forEach");
                    return $"kt_forEach({r}, {Arg(a[0], unitLambda: true, implicitIt: true)})";

                case "map" or "filter" or "any" or "all" or "none" or "sumOf" when a.Count == 1:
                    Need("kt_" + node.Name);
                    return $"kt_{node.Name}({r}, {Arg(a[0], implicitIt: true)})";

                case "count" when a.Count == 1:
                    Need("kt_count");
                    return $"kt_count({r}, {Arg(a[0], implicitIt: true)})";

                case "count": return $"((int){r}.size())";

                // ----- числа и строки -----
                case "toInt": Need("kt_toInt"); return $"kt_toInt({r})";
                case "toLong": Need("kt_toLong"); return $"kt_toLong({r})";
                case "toDouble": Need("kt_toDouble"); return $"kt_toDouble({r})";
                case "toString": Need("kt_str"); return $"kt_str({r})";
                case "uppercase": Need("kt_upper"); return $"kt_upper({r})";
                case "lowercase": Need("kt_lower"); return $"kt_lower({r})";
                case "startsWith": Need("kt_startsWith"); return $"kt_startsWith({r}, {A(0)})";
                case "endsWith": Need("kt_endsWith"); return $"kt_endsWith({r}, {A(0)})";
                case "split": Need("kt_split"); return $"kt_split({r}, {A(0)})";
                case "substring" when a.Count == 1: return $"{r}.substr({A(0)})";
                case "substring" when a.Count == 2: return $"{r}.substr({A(0)}, ({A(1)}) - ({A(0)}))";
            }

            return $"{r}.{node.Name}({string.Join(", ", a.Select(x => Arg(x)))})";
        }

        // ---------- инструкции ----------

        public void VisitExprStmt(ExprStmt node)
        {
            string text = Render(node.Expression);
            if (!string.IsNullOrEmpty(text))
            {
                _w.Line($"{text};");
            }
        }

        public void VisitVarDecl(VarDeclStmt node) => _w.Line(DeclText(node) + ";");

        public void VisitAssign(AssignStmt node) => _w.Line(AssignText(node) + ";");

        public void VisitIncDec(IncDecStmt node) => _w.Line(IncDecText(node) + ";");

        // Тексты без ';' — переиспользуются в заголовке for (...)
        private string DeclText(VarDeclStmt node)
        {
            string declared = DeclaredType(node);

            // val для изменяемых коллекций (mutableListOf, IntArray...) — НЕ const, иначе add() не скомпилируется
            string prefix = node.IsConst && !IsMutableCollection(node) ? "const " : "";

            string text;
            if (node.Init == null)
            {
                text = $"{prefix}{declared} {node.Name}";
            }
            else
            {
                _expectedType = node.Type;
                string init = Render(node.Init);
                _expectedType = null;
                text = $"{prefix}{declared} {node.Name} = {init}";
            }

            RememberVarType(node);
            return text;
        }

        private static bool IsMutableCollection(VarDeclStmt node)
        {
            if (IsMutableListType(node.Type)) return true;
            return node.Init is CallExpr { Receiver: null } call && MutableCtors.Contains(call.Name);
        }

        private void RememberVarType(VarDeclStmt node)
        {
            string type = node.Type != null && node.Type.Kind != DataType.Unknown
                ? ToCppType(node.Type)
                : (node.Init != null ? InferType(node.Init) : null);

            if (type != null && type != "auto") _varTypes[node.Name] = type;
        }

        // Тип не указан (Kotlin, C# var) -> auto
        private static string DeclaredType(VarDeclStmt node)
        {
            if (node.Type == null) return "auto";
            if (node.Type.Kind != DataType.Unknown) return ToCppType(node.Type);
            return node.Init == null ? node.Type.SourceName : "auto";
        }

        private string AssignText(AssignStmt node) => $"{node.Target} {node.Op} {Render(node.Value)}";

        private static string IncDecText(IncDecStmt node) => node.Target + (node.IsIncrement ? "++" : "--");

        private string Inline(Stmt stmt) => stmt switch
        {
            VarDeclStmt v => DeclText(v),
            AssignStmt a => AssignText(a),
            IncDecStmt i => IncDecText(i),
            ExprStmt e => Render(e.Expression),
            _ => string.Empty
        };

        public void VisitBlock(BlockStmt node)
        {
            _w.Line("{");
            _w.Indent();
            foreach (var stmt in node.Statements) Emit(stmt);
            _w.Dedent();
            _w.Line("}");
        }

        public void VisitIf(IfStmt node) => EmitIf(node, "if");

        // Цепочка else-if печатается плоско: "else if (...)" на одном уровне отступа
        private void EmitIf(IfStmt node, string keyword)
        {
            _w.Line($"{keyword} ({Render(node.Condition)})");

            // if (a) if (b) x; else y  — в C++ else прицепится к внутреннему if, поэтому оборачиваем в скобки
            if (node.Else != null && node.Then is IfStmt)
            {
                _w.Line("{");
                _w.Indent();
                Emit(node.Then);
                _w.Dedent();
                _w.Line("}");
            }
            else
            {
                EmitBody(node.Then);
            }

            if (node.Else == null) return;

            if (node.Else is IfStmt elseIf)
            {
                EmitIf(elseIf, "else if");
                return;
            }

            _w.Line("else");
            EmitBody(node.Else);
        }

        public void VisitForRange(ForRangeStmt node)
        {
            string v = node.Variable;
            _w.Line($"for (int {v} = {Render(node.Start)}; {v} <= {Render(node.EndInclusive)}; {v}++)");
            EmitBody(node.Body);
        }

        public void VisitFor(ForStmt node)
        {
            _w.Line($"for ({Inline(node.Init)}; {Render(node.Condition)}; {Inline(node.Update)})");
            EmitBody(node.Body);
        }

        public void VisitForEach(ForEachStmt node)
        {
            string type = node.VariableType != null && node.VariableType.Kind != DataType.Unknown
                ? ToCppType(node.VariableType)
                : "const auto&";
            _w.Line($"for ({type} {node.Variable} : {Render(node.Iterable)})");
            EmitBody(node.Body);
        }

        public void VisitWhile(WhileStmt node)
        {
            _w.Line($"while ({Render(node.Condition)})");
            if (node.Body != null) EmitBody(node.Body);
            else _w.Line(";");
        }

        public void VisitDoWhile(DoWhileStmt node)
        {
            _w.Line("do");
            if (node.Body != null) EmitBody(node.Body);
            else _w.Line(";");
            _w.Line($"while ({Render(node.Condition)});");
        }

        public void VisitFunction(FunctionDecl node)
        {
            bool isMain = node.Name == "main";
            bool deducedReturn = !isMain && node.ReturnType is { Kind: DataType.Unknown } && node.IsExpressionBody;

            // main в C++ всегда возвращает int и не принимает args
            string returnType = isMain ? "int" : ToCppType(node.ReturnType);
            var parameters = isMain ? new List<Parameter>() : node.Parameters.ToList();

            foreach (var p in parameters)
            {
                string t = ToCppType(p.Type);
                if (t != "auto") _varTypes[p.Name] = t;
            }

            // Значения по умолчанию — только в прототипе; в определении их повторять нельзя
            string withDefaults = string.Join(", ", parameters.Select(p =>
                $"{ParamType(p.Type)} {p.Name}" + (p.Default != null ? $" = {Render(p.Default)}" : "")));
            string plain = string.Join(", ", parameters.Select(p => $"{ParamType(p.Type)} {p.Name}"));

            // fun f(x: Int) = x * 2 без типа -> auto; прототип с auto до определения невозможен, поэтому без него
            if (!isMain && !deducedReturn) _prototypes.Add($"{returnType} {node.Name}({withDefaults});");
            _w.Line($"{returnType} {node.Name}({(deducedReturn ? withDefaults : plain)})");

            if (node.Body != null)
            {
                if (isMain && !node.IsExpressionBody)
                {
                    // в конец main гарантированно добавляем return 0
                    _w.Line("{");
                    _w.Indent();
                    foreach (var stmt in node.Body.Statements) Emit(stmt);
                    _w.Line("return 0;");
                    _w.Dedent();
                    _w.Line("}");
                }
                else
                {
                    Emit(node.Body);
                }
            }

            _w.Line("");
        }

        public void VisitReturn(ReturnStmt node)
        {
            _w.Line(node.Value == null ? "return;" : $"return {Render(node.Value)};");
        }

        public void VisitBreak(BreakStmt node) => _w.Line("break;");

        public void VisitContinue(ContinueStmt node) => _w.Line("continue;");

        public void VisitThrow(ThrowStmt node)
        {
            _w.Line($"throw {Render(node.Value)};");
        }

        public void VisitTry(TryStmt node)
        {
            _w.Line("try");
            Emit(node.Body);

            foreach (var clause in node.Catches)
            {
                if (string.IsNullOrEmpty(clause.ExceptionType))
                {
                    _w.Line("catch (...)");
                    Emit(clause.Body);
                    continue;
                }

                string cppType = clause.ExceptionType switch
                {
                    "Exception" => "const std::exception&",
                    "IOException" => "const std::ios_base::failure&",
                    _ => "const std::exception&"
                };

                _w.Line($"catch ({cppType} {clause.ParamName})");
                Emit(clause.Body);
            }

            if (node.Finally != null)
            {
                _w.Line("/* finally */");
                Emit(node.Finally);
            }
        }

        public void VisitUnsupported(UnsupportedStmt node)
        {
            _w.Line($"/* {node.Description}: {node.SourceText} */");
        }
    }
}