# marine-lang
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

## EBNF

```ebnf
program        = {func_definition} ;

func_definition
               = 'func' , id , variable_list , func_body , 'end' ;
func_body      = {statement} ;
statement      =  ret_statement |
                  assignment |
                  re_assignment |
                  expr ;
ret_statement  = 'ret' , expr ;
assignment     = 'let' , re_assignment ;
re_assignment  = id , '=' , expr ;
expr           = func_call | 
                 float_literal | 
                 int_literal | 
                 bool_literal | 
                 char_literal | 
                 string_literal |
                 variable ;
func_call      = id , param_list ;
param_list     = '(' , [ expr , { ',' , expr } ] , ')' ;
variable_list  = '(' , [ variable , { ',' , variable } ] , ')' ;


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


スキップ

skip           = ' ' | '\n' | '\r' | '\t' ;
```
