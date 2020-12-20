# marine-lang
![Tests](https://github.com/MineMonsterLabo/marine-lang/workflows/.NET%20Core/badge.svg)

[![Nuget](https://img.shields.io/nuget/v/MarineLang.svg)](https://www.nuget.org/packages/MarineLang/)

このプロジェクトは宝鐘マリンを応援します

[Youtubeチャンネル](https://www.youtube.com/channel/UCCzUftO8KOVkV4wQG1vkUvg)
## このプロジェクトって？

Unityで簡易でパワフルなスクリプト言語を動かしたい！(特に非同期周り)

## 言語の特徴(今後の未来)

- luaよりもタイプ数を少なく
- 非同期やコルーチンの専用構文
- ハンガリアン記法による簡単な静的型チェック
- 他にも静的型チェックは用意する
- if式あるよ
- ブレークポイントのサポート
## Code Example

```
fun sum(min, max)  
    ret 
        if min == max 
        {
            min
        } 
        else {
	    min + sum(min + 1, max)
        }
end
```

## 字句規約

コメントは`//`で始まり、その行の終わりまでコメントアウトされます。

`/*`と`*/`で囲んだ部分を範囲コメントアウトできます。

## EBNF

```ebnf
program        = {func_definition} ;
func_definition
               = 'func' , id , variable_list , func_body , 'end' ;
func_body      = {statement} ;
statement      =  
                  yield_statement |
                  while_statement |
                  for_statement |
                  ret_statement |
                  assignment |
                  field_assignment |
                  re_assignment_variable |
                  re_assignment_indexer |
                  expr ;
while_statement
               = 'while' , expr , block ;
for_statement  = 'for' , variable, '=', expr  ',' , expr , ',' , expr , block ;
ret_statement  = 'ret' , expr ;
assignment     = 'let' , re_assignment_variable ;
field_assignment  
               = indexer_op_expr ,  dot_terms , '.' , variable , '=' , expr ;
re_assignment_variable  =  variable , '=' , expr ;
re_assignment_indexer  
               = term , indexers , '=' , expr ;
yield_statement
               = 'yield' ; 
expr           = if_expr | binary_op_expr ;
if_expr        = 'if' , expr , block , [ 'else' , block ] ;
block          = '{' , {statement} , '}'
binary_op_expr = unary_op_expr , [binary_op , binary_op_expr] ;
unary_op_expr  = { unary_op } , dot_op_expr ;
dot_op_expr    = indexer_op_expr , dot_terms ;
indexer_op_expr
               = term , [indexers] ;
dot_terms      = { '.' , field_term , [indexers] } ;
field_term     = 'await' | func_call | variable ;
term           =
                 '(' , expr , ')' |
                 func_call | 
                 float_literal | 
                 int_literal | 
                 bool_literal | 
                 char_literal | 
                 string_literal |
                 array_literal |
                 action_literal |
                 variable ;
action_literal = '{' , action_variable_list , func_body , '}' ;
func_call      = id , param_list ;
indexers       = ( '[' , expr , ']' )+ ;
param_list     = '(' , [ expr , { ',' , expr } ] , ')' ;
variable_list  = '(' , [ variable , { ',' , variable } ] , ')' ;
action_variable_list  = '|' , [ variable , { ',' , variable } ] , '|' ;
array_literal  = '[' [ expr , { ',' , expr } ] , [ ';' , int_literal ] , ']'


トークン

float_literal  = int_literal , '.' , int_literal ;
int_literal    = digit ;
bool_literal   = 'true' | 'false' ;
char_literal   = ? 省略 ? ;
string_literal = ? 省略 ? ;
variable       = id ;
id             = lower_letter , {id_char} ;
id_char        = digit | lower_letter | '_' ;
lower_letter   = ? 省略 ?;
digit          = '0' | '1' | '2' | '3' | '4' | '5' | '6' | '7' | '8' | '9' ;
binary_op      = '<' | '<=' | '>' | '>=' | '&&' | '||' | '==' | '!=' | '+' | '-' | '*' | '/' | '%' ;
unary_op      = '-' | '!' ;


スキップ

skip           = ' ' | '\n' | '\r' | '\t' ;
```
