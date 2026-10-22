lexer grammar KotlinLexer;

@header {
namespace Kotlin_plus_plus;
}

DelimitedComment:
	'/*' (DelimitedComment | .)*? '*/' -> channel(HIDDEN);
LineComment: '//' ~[\r\n]* -> channel(HIDDEN);
WS: [\u0020\u0009\u000C] -> channel(HIDDEN);
NL: '\n' | '\r' '\n'?;

fragment Hidden: DelimitedComment | LineComment | WS;

IF: 'if';
ELSE: 'else';
FOR: 'for';
AND: 'and';
OR: 'or';
IN: 'in';
VAL: 'val';
VAR: 'var';
WHILE: 'while';
THROW: 'throw';
RETURN: 'return';
CONTINUE: 'continue';
BREAK: 'break';
FUN: 'fun';
TRY: 'try';
CATCH: 'catch';
FINALLY: 'finally';
DO: 'do';
WHEN: 'when';
UNTIL: 'until';
DOWNTO: 'downTo';
STEP: 'step';

LPAREN: '(';
RPAREN: ')';
LCURL: '{';
RCURL: '}';
MULT: '*';
MOD: '%';
DIV: '/';
ADD: '+';
SUB: '-';
INCR: '++';
DECR: '--';
CONJ: '&&';
DISJ: '||';
EXCL_NO_WS: '!';
COLON: ':';
SEMICOLON: ';';
ASSIGNMENT: '=';
ADD_ASSIGNMENT: '+=';
SUB_ASSIGNMENT: '-=';
MULT_ASSIGNMENT: '*=';
DIV_ASSIGNMENT: '/=';
MOD_ASSIGNMENT: '%=';
HASH: '#';
DOT: '.';
COMMA: ',';
DOTDOT: '..';
ARROW: '->';
LBRACKET: '[';
RBRACKET: ']';
EXCL_EXCL: '!!';
QUEST_DOT: '?.';
QUEST: '?';

LANGLE: '<';
RANGLE: '>';
LE: '<=';
GE: '>=';
EXCL_EQ: '!=';
EQEQ: '==';

STRING_LITERAL
    : '"' StringPart* '"'
    ;
fragment StringPart
    : ~["\\$]
    | '\\' .
    | '$'
    | TemplateExpr
    ;
// ${ ... } может содержать вложенные строки и фигурные скобки: "${xs.joinToString(", ")}"
fragment TemplateExpr
    : '$' '{' TemplateInner* '}'
    ;
fragment TemplateInner
    : NestedString
    | '{' TemplateInner* '}'
    | ~[{}"]
    ;
fragment NestedString
    : '"' ( ~["\\] | '\\' . )* '"'
    ;
CHAR_LITERAL
    : '\'' ( ~['\\\r\n] | '\\' . ) '\''
    ;
ID: [a-zA-Z_][a-zA-Z0-9_]*;
LONG: [0-9]+ 'L';
INT: [0-9]+;
DOUBLE: [0-9]+ '.' [0-9]+ [fF]?;
