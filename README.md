# marine-lang
このプロジェクトは宝鐘マリンを応援します

[Youtubeチャンネル](https://www.youtube.com/channel/UCCzUftO8KOVkV4wQG1vkUvg)
## このプロジェクトって？

Unityで簡易でパワフルなスクリプト言語を動かしたい！(特に非同期周り)

## BNF

`[]`は0~1回のみ

`{}`は0回以上

```
:program       := :skip_many {:func_definition} :skip_many
:skip_many     := {:skip}
:skip          := ' ' | '\n' | '\r' | '\t'
:func_definition      
               := 'func' :skip_many :id :skip_many '(' :skip_many ')' 
                  :skip_many :func_body 
                  'end'
:func_body     := {:statement :skip_many}
:statement     := expr | ret_statement
:ret_statement := 'ret' :skip_many :expr
:expr          := :func_call | 
                  :float_literal | 
                  :int_literal | 
                  :bool_literal | 
                  :char_literal | 
                  :string_literal
:func_call     := :id :skip_many :param_list
:float_literal := :int_literal '.' :int_literal
:int_literal   := [0-9]+
:bool_literal  := 'true' | 'false'
:char_literal  := "省略"
:string_literal:= "省略"
:param_list    := '(' :skip_many [ :expr :skip_many {',' :skip_many :expr} :skip_many ] ')'
:id            := :lower_letter {id_char}
:id_char       := :digit | :lower_letter | '_';
:lower_letter  := "省略"
:digit  := "省略"
```