parser grammar KotlinParser;

options {
	tokenVocab = KotlinLexer;
}

@header {
namespace Kotlin_plus_plus;
}

root: statements EOF;

statements:
	(SEMICOLON | NL)* (statement ((SEMICOLON | NL)+ statement)*)? (
		SEMICOLON
		| NL
	)*;

statement:
	variableDeclaration
	| assignment
	| ifExpression
	| forStatement
	| block
	| whileStatement
	| doWhileStatement
	| tryExpression
	| jumpExpression
	| whenExpression
	| expression (SEMICOLON | NL)*
	| functionDeclaration
	| unparsedStatement;

// Порядок альтернатив = приоритет (выше в списке -> теснее связывает).
expression:
	LPAREN NL* expression NL* RPAREN														# Parens
	| expression NL* (DOT | QUEST_DOT) NL* ID												# MemberAccess
	| expression LPAREN NL* (expression (NL* COMMA NL* expression)* NL*)? RPAREN			# FunctionCall
	| expression lambdaLiteral																# TrailingLambda
	| expression LBRACKET NL* expression NL* RBRACKET										# Index
	| expression EXCL_EXCL																	# NotNullAssert
	| SUB expression																		# UnaryMinus
	| EXCL_NO_WS expression																	# UnaryNot
	| expression (MULT | DIV | MOD) NL* expression											# MulDiv
	| expression (ADD | SUB) NL* expression													# AddSub
	| expression (DOTDOT | UNTIL | DOWNTO) expression										# RangeExp
	| expression STEP expression															# StepExp
	| expression IN expression																# InExp
	| expression EXCL_NO_WS IN expression													# NotInExp
	| expression (LANGLE | RANGLE | LE | GE | EQEQ | EXCL_EQ) NL* expression				# Comparison
	| expression AND NL* expression															# AndOp
	| expression OR NL* expression															# OrOp
	| expression CONJ NL* expression														# AndLogical
	| expression DISJ NL* expression														# OrLogical
	| ID typeArguments LPAREN NL* (expression (NL* COMMA NL* expression)* NL*)? RPAREN		# GenericCall
	| IF NL* LPAREN NL* expression NL* RPAREN NL* expression NL* ELSE NL* expression		# IfExp
	| whenExpression																		# WhenAsExpr
	| lambdaLiteral																			# LambdaExp
	| ID																					# Identifier
	| INT																					# IntLiteral
	| LONG																					# LongLiteral
	| DOUBLE																				# DoubleLiteral
	| STRING_LITERAL																		# StringLiteral
	| CHAR_LITERAL																			# CharLiteral;

// ---------- типы ----------
kotlinType: ID typeArguments? QUEST?;

typeArguments: LANGLE NL* kotlinType (NL* COMMA NL* kotlinType)* NL* RANGLE;

// ---------- лямбды ----------
// { x, y -> ... }   |   { it * 2 }
lambdaLiteral: LCURL NL* (lambdaParams ARROW NL*)? statements RCURL;

lambdaParams: lambdaParam (COMMA lambdaParam)*;

lambdaParam: ID (COLON kotlinType)?;

// ---------- when ----------
whenExpression:
	WHEN NL* (LPAREN NL* expression NL* RPAREN)? NL* LCURL NL* (whenEntry NL*)* RCURL;

whenEntry:
	whenCondition (NL* COMMA NL* whenCondition)* NL* ARROW NL* controlStructureBody
	| ELSE NL* ARROW NL* controlStructureBody;

whenCondition:
	IN expression		# WhenInCondition
	| expression		# WhenValueCondition;

// ---------- прочее (как было) ----------
fallbackToken:
    ~(SEMICOLON | NL | LCURL | RCURL | VAL | VAR | FUN);

unknownTail: anyUnknownBlock? ;

anyUnknownBlock: LCURL unknownBlockContent* RCURL;

unknownBlockContent:
    anyUnknownBlock
    | ~(LCURL | RCURL);

unparsedStatement:
	(~(NL | SEMICOLON | LCURL | RCURL))+ (SEMICOLON | NL)*;

block: LCURL NL* statements NL* RCURL;

controlStructureBody: block | statement;

ifExpression:
	IF NL* LPAREN NL* expression NL* RPAREN NL* controlStructureBody (
		NL* ELSE NL* controlStructureBody
	)?;

// for (i in 0..n) / until / downTo / step / for (x in xs) / for (i in xs.indices)
forStatement:
	FOR NL* LPAREN NL* ID IN expression NL* RPAREN NL* controlStructureBody;

// val x = 1 | var x: Int = 1 | val xs: List<Int> = listOf(1, 2) | var x: Int
variableDeclaration: (VAL | VAR) ID (COLON kotlinType)? (ASSIGNMENT NL* expression)?;

whileStatement:
	WHILE NL* LPAREN NL* expression NL* RPAREN NL* (
		controlStructureBody
		| SEMICOLON
	);

doWhileStatement:
	DO NL* controlStructureBody? NL* WHILE NL* LPAREN NL* expression RPAREN;

jumpExpression:
	THROW NL* expression
	| RETURN expression?
	| CONTINUE
	| BREAK;

tryExpression:
	TRY NL* block (
		(NL* catchBlock)+ (NL* finallyBlock)?
		| NL* finallyBlock
	);

catchBlock: CATCH NL* LPAREN ID COLON ID RPAREN NL* block;

finallyBlock: FINALLY NL* block;

functionDeclaration:
	FUN NL* ID NL* LPAREN NL* (
		parameter (NL* COMMA NL* parameter)*
	)? NL* RPAREN (NL* COLON NL* kotlinType)? NL* functionBody;

parameter: ID NL* COLON NL* kotlinType (NL* ASSIGNMENT NL* expression)?;

functionBody: block | ASSIGNMENT NL* expression;

assignment: ID ASSIGNMENT NL* expression										# NormalAssignment
	| ID ADD_ASSIGNMENT NL* expression											# AddAssignment
	| ID SUB_ASSIGNMENT NL* expression											# SubAssignment
	| ID MULT_ASSIGNMENT NL* expression											# MultAssignment
	| ID DIV_ASSIGNMENT NL* expression											# DivAssignment
	| ID MOD_ASSIGNMENT NL* expression											# ModAssignment
	| ID INCR																	# PostIncrement
	| ID DECR																	# PostDecrement
	| ID LBRACKET expression RBRACKET (
		ASSIGNMENT
		| ADD_ASSIGNMENT
		| SUB_ASSIGNMENT
		| MULT_ASSIGNMENT
		| DIV_ASSIGNMENT
		| MOD_ASSIGNMENT
	) NL* expression															# IndexAssignment;
