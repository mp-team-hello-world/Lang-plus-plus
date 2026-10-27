parser grammar CSharpParser;

options {
	tokenVocab = CSharpLexer;
}

@header {
namespace Kotlin_plus_plus;
}

root: statements EOF;

statements: statement*;

statement:
	usingDirective
	| namespaceDeclaration
	| classDeclaration
	| variableDeclaration
	| assignment SEMICOLON
	| ifStatement
	| forStatement
	| foreachStatement
	| whileStatement
	| doWhileStatement
	| tryStatement
	| jumpStatement
	| expression SEMICOLON
	| functionDeclaration
	| block
	| SEMICOLON
	| unparsedStatement;

// int, string, Foo, а также массивы: string[]
type:
	(
		INT_TYPE
		| LONG_TYPE
		| FLOAT_TYPE
		| DOUBLE_TYPE
		| BOOL_TYPE
		| CHAR_TYPE
		| STRING_TYPE
		| VOID
		| ID
	) (LBRACK RBRACK)?;

// using System;   (using (...) { } и using static остаются в unparsedStatement)
usingDirective: USING ID (DOT ID)* SEMICOLON;

// namespace A.B { ... }   или   namespace A.B;
namespaceDeclaration: NAMESPACE ID (DOT ID)* (block | SEMICOLON);

// class Program { ... } — члены класса "вытаскиваются" на верхний уровень (см. CSharpAstBuilder)
classDeclaration: modifier* CLASS ID block;

variableDeclaration:
	modifier* (CONST? type | VAR) ID (ASSIGNMENT expression)? SEMICOLON;

assignment:
	ID ASSIGNMENT expression		# NormalAssignment
	| ID ADD_ASSIGNMENT expression	# AddAssignment
	| ID SUB_ASSIGNMENT expression	# SubAssignment
	| ID MULT_ASSIGNMENT expression	# MultAssignment
	| ID DIV_ASSIGNMENT expression	# DivAssignment;

block: LCURL statements RCURL;

controlStructureBody: block | statement;

ifStatement:
	IF LPAREN expression RPAREN controlStructureBody (ELSE controlStructureBody)?;

forStatement:
	FOR LPAREN forInit? SEMICOLON expression? SEMICOLON forUpdate? RPAREN controlStructureBody;

forInit: variableDeclarationNoSemi | assignment | expression;

variableDeclarationNoSemi:
	(CONST? type | VAR) ID (ASSIGNMENT expression)?;

forUpdate: assignment | expression;

foreachStatement:
	FOREACH LPAREN (VAR | type) ID IN expression RPAREN controlStructureBody;

whileStatement:
	WHILE LPAREN expression RPAREN controlStructureBody;

doWhileStatement:
	DO controlStructureBody WHILE LPAREN expression RPAREN SEMICOLON;

tryStatement:
	TRY block (catchBlock)+ (finallyBlock)?
	| TRY block finallyBlock;

catchBlock:
	CATCH (LPAREN type ID? RPAREN)? block;

finallyBlock:
	FINALLY block;

jumpStatement:
	RETURN expression? SEMICOLON
	| THROW expression SEMICOLON
	| BREAK SEMICOLON
	| CONTINUE SEMICOLON;

modifier: PUBLIC | PRIVATE | PROTECTED | INTERNAL | STATIC;

functionDeclaration:
	modifier* type ID LPAREN parameterList? RPAREN (block | ARROW expression SEMICOLON);

parameterList:
	parameter (COMMA parameter)*;

parameter:
	type ID;

expression:
	LPAREN expression RPAREN												# Parens
	| expression LPAREN (expression (COMMA expression)*)? RPAREN			# FunctionCall
	| expression DOT ID														# MemberAccess
	| SUB expression														# UnaryMinus
	| EXCL_NO_WS expression													# UnaryNot
	| INCR expression														# PreIncrement
	| DECR expression														# PreDecrement
	| expression INCR														# PostIncrement
	| expression DECR														# PostDecrement
	| expression (MULT | DIV | MOD) expression								# MulDiv
	| expression (ADD | SUB) expression										# AddSub
	| expression (LANGLE | RANGLE | LE | GE | EQEQ | EXCL_EQ) expression	# Comparison
	| expression CONJ expression											# AndLogical
	| expression DISJ expression											# OrLogical
	| NEW ID LPAREN (expression (COMMA expression)*)? RPAREN	# NewObject
	| ID																	# Identifier
	| INT																	# IntLiteral
	| DOUBLE																# DoubleLiteral
	| STRING_LITERAL														# StringLiteral
	| CHAR_LITERAL															# CharLiteral
	| BOOL_LITERAL															# BoolLiteral;

unparsedStatement:
	(~(SEMICOLON | LCURL | RCURL))+ SEMICOLON?;

