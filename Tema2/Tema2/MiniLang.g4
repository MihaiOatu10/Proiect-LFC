grammar MiniLang;

INT: 'int';
FLOAT: 'float';
DOUBLE: 'double';
STRING_TYPE: 'string';
CONST: 'const';
VOID: 'void';
IF: 'if';
ELSE: 'else';
FOR: 'for';
WHILE: 'while';
RETURN: 'return';

AND: '&&';
OR: '||';
NOT: '!';

EQ: '==';
NEQ: '!=';
LTE: '<=';
GTE: '>=';
LT: '<';
GT: '>';

ASSIGN: '=';
ADD_ASSIGN: '+=';
SUB_ASSIGN: '-=';
MUL_ASSIGN: '*=';
DIV_ASSIGN: '/=';
MOD_ASSIGN: '%=';

INC: '++';
DEC: '--';

ADD: '+';
SUB: '-';
MUL: '*';
DIV: '/';
MOD: '%';

LPAREN: '(';
RPAREN: ')';
LBRACE: '{';
RBRACE: '}';
COMMA: ',';
SEMI: ';';

BOOL: 'true' | 'false';
ID: [a-zA-Z_][a-zA-Z0-9_]*;
INT_LIT: [0-9]+;
FLOAT_LIT: [0-9]+ '.' [0-9]+ ('f'|'F')?;
STRING_LIT: '"' .*? '"';

WS: [ \t\r\n]+ -> skip;
LINE_COMMENT: '//' ~[\r\n]* -> skip;
BLOCK_COMMENT: '/*' .*? '*/' -> skip;

program: globalDecl* EOF;

globalDecl
    : varDecl       # GlobalVarDeclaration
    | funcDecl      # GlobalFuncDeclaration
    ;

varDecl
    : type ID (ASSIGN expression)? SEMI
    | CONST type ID ASSIGN expression SEMI
    ;

funcDecl
    : type ID LPAREN paramList? RPAREN block
    | VOID ID LPAREN paramList? RPAREN block
    ;

paramList
    : param (COMMA param)*
    ;

param
    : type ID
    ;

block
    : LBRACE statement* RBRACE
    ;

statement
    : varDecl                       # LocalVarStmt
    | assignment SEMI               # AssignStmt
    | IF LPAREN expression RPAREN block (ELSE block)? # IfStmt
    | FOR LPAREN (varDecl | assignment)? SEMI expression? SEMI assignment? RPAREN block # ForStmt
    | WHILE LPAREN expression RPAREN block  # WhileStmt
    | RETURN expression? SEMI       # ReturnStmt
    | funcCall SEMI                 # FuncCallStmt
    | block                         # BlockStmt
    | expression SEMI               # ExprStmt
    ;

assignment
    : ID (ASSIGN | ADD_ASSIGN | SUB_ASSIGN | MUL_ASSIGN | DIV_ASSIGN | MOD_ASSIGN) expression
    | ID (INC | DEC)
    ;

expression
    : expression (MUL | DIV | MOD) expression   # MulDivExpr
    | expression (ADD | SUB) expression         # AddSubExpr
    | expression (LT | GT | LTE | GTE) expression # RelExpr
    | expression (EQ | NEQ) expression          # EqExpr
    | expression AND expression                 # AndExpr
    | expression OR expression                  # OrExpr
    | NOT expression                            # NotExpr
    | atom                                      # AtomExpr
    ;

atom
    : LPAREN expression RPAREN
    | funcCall
    | ID
    | INT_LIT
    | FLOAT_LIT
    | STRING_LIT
    | BOOL
    ;

funcCall
    : ID LPAREN argsList? RPAREN
    ;

argsList
    : expression (COMMA expression)*
    ;

type
    : INT | FLOAT | DOUBLE | STRING_TYPE
    ;