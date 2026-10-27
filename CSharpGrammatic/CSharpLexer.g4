lexer grammar CSharpLexer;

@header {
namespace Kotlin_plus_plus;
}

// Comments
DelimitedComment: '/*' .*? '*/' -> channel(HIDDEN);
LineComment: '//' ~[\r\n]* -> channel(HIDDEN);
WS: [ \t\r\n\u000C]+ -> channel(HIDDEN);

// Keywords
IF: 'if';
ELSE: 'else';
FOR: 'for';
FOREACH: 'foreach';
WHILE: 'while';
DO: 'do';
IN: 'in';
VAR: 'var';
CONST: 'const';
RETURN: 'return';
CONTINUE: 'continue';
BREAK: 'break';
THROW: 'throw';
TRY: 'try';
CATCH: 'catch';
FINALLY: 'finally';
USING: 'using';
NAMESPACE: 'namespace';
CLASS: 'class';
NEW: 'new';

// Modifiers
PUBLIC: 'public';
PRIVATE: 'private';
PROTECTED: 'protected';
STATIC: 'static';
INTERNAL: 'internal';

// Primitive types
VOID: 'void';
INT_TYPE: 'int';
LONG_TYPE: 'long';
FLOAT_TYPE: 'float';
DOUBLE_TYPE: 'double';
BOOL_TYPE: 'bool';
CHAR_TYPE: 'char';
STRING_TYPE: 'string';

// Symbols & Punctuation
LPAREN: '(';
RPAREN: ')';
LCURL: '{';
RCURL: '}';
LBRACK: '[';
RBRACK: ']';
SEMICOLON: ';';
COLON: ':';
COMMA: ',';
DOT: '.';
ARROW: '=>';

// Assignments
ASSIGNMENT: '=';
ADD_ASSIGNMENT: '+=';
SUB_ASSIGNMENT: '-=';
MULT_ASSIGNMENT: '*=';
DIV_ASSIGNMENT: '/=';

// Arithmetic operators
MULT: '*';
DIV: '/';
MOD: '%';
ADD: '+';
SUB: '-';
INCR: '++';
DECR: '--';

// Logical operators
CONJ: '&&';
DISJ: '||';
EXCL_NO_WS: '!';

// Comparison operators
EQEQ: '==';
EXCL_EQ: '!=';
LANGLE: '<';
RANGLE: '>';
LE: '<=';
GE: '>=';

// Literals & Identifiers
BOOL_LITERAL: 'true' | 'false';
STRING_LITERAL: '"' (~["\\\r\n] | '\\' .)* '"';
CHAR_LITERAL: '\'' (~['\\\r\n] | '\\' .)* '\'';
INT: [0-9]+;
DOUBLE: [0-9]+ '.' [0-9]+;
ID: [a-zA-Z_][a-zA-Z0-9_]*;

