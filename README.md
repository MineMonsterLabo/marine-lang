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

## BNF

`[]`は0~1回のみ

`{}`は0回以上

```
:program       := :skip_many {:func_definition} :skip_many
:skip_many     := {:skip}
:skip          := ' ' | '\n' | '\r' | '\t'
:func_definition      
               := 'func' :skip_many :id :skip_many :variable_list 
                  :skip_many :func_body 
                  'end'
:func_body     := {:statement :skip_many}
:statement     := :ret_statement |
                  :assignment |
                  :re_assignment |
                  :expr | 
:ret_statement := 'ret' :skip_many :expr
:assignment    := 'let' :skip_many :id :skip_many '=' :skip_many :expr
:re_assignment := :id :skip_many '=' :skip_many :expr
:expr          := :func_call | 
                  :float_literal | 
                  :int_literal | 
                  :bool_literal | 
                  :char_literal | 
                  :string_literal |
                  :variable
:func_call     := :id :skip_many :param_list
:float_literal := :int_literal '.' :int_literal
:int_literal   := [0-9]+
:bool_literal  := 'true' | 'false'
:char_literal  := "省略"
:string_literal:= "省略"
:variable      := :id
:param_list    := '(' :skip_many [ :expr :skip_many {',' :skip_many :expr} :skip_many ] ')'
:variable_list := '(' :skip_many [ :variable :skip_many {',' :skip_many :variable} :skip_many ] ')'
:id            := :lower_letter {id_char}
:id_char       := :digit | :lower_letter | '_';
:lower_letter  := "省略"
:digit  := "省略"
```
