# marine-lang
![Tests](https://github.com/MineMonsterLabo/marine-lang/workflows/.NET%20Core/badge.svg)

[![Nuget](https://img.shields.io/nuget/v/MarineLang.svg)](https://www.nuget.org/packages/MarineLang/)

## このプロジェクトって？

Unityで簡易でパワフルなスクリプト言語を動かしたい！(特に非同期周り)

## 言語の特徴(今後の未来)

- luaよりもタイプ数を少なく
- 非同期やコルーチンの専用構文
- ハンガリアン記法による簡単な静的型チェック(要検討)
- 他にも静的型チェックは用意する(要検討)
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
foreach_statement  
               = 'foreach' , variable, 'in', expr , block ;
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

## 言語仕様

### 型システム
マリン言語は静的型付けではありません。
全ての型は**ダイナミック**に決定されます。
複雑な用途に使う予定はないので、動的型付けで十分と判断しました。

### 関数の定義

#### 例
```
fun hoge_fuga(abc, def)
   ret abc + def
end
```

引数を取ることができ、`ret`で値を返すことが出来ます。
定義した`hoge_fuga`関数はC#から呼び出すことも出来ます。

### 変数宣言

#### 例
```
let hoge = "hello world"
```
変数宣言は、必ず初期値を`=`で代入する必要があります。
何故なら、普通変数は初期化して使うケースが多いからです。
(わざわざ未初期化の変数を作る機能は無駄と判断し、実装しませんでした)

### 数値

`0`,`-10`,`123`
これらはC#の`System.Int32`として扱われます。

`1.2`,`-5.4`,` 3.3333214`
これらはC#の`System.Single`として扱われます。


### 文字列

`"hello world"`,`"あいうえお"`,`"改行\r\nされるよ"`
これらはC#の`System.String`として扱われます。

### 真偽値

`true`,`false`
これらはC#の`System.Boolean`として扱われます。

### 二項演算子
以下のものが二項演算子として使えます。

`+`,`-`,`*`,`/`,`%`,`&&`,`||`,`==`,`!=`,`<`,`>`,`<=`,`>=`

今の所は演算子のオーバーロードに対応しておらず、`int`,`bool`,`float`,`string`の型にしか対応していません。

### 単項演算子
以下のものが単項演算子として使えます。
`-`,`!`
こちらも演算子のオーバーロードに対応してません。

### 条件分岐
#### 例
```
if(4<7) { "ok" }
```

```
if(4<7) { "ok" } else { "error" }
```

```
let message = if(4<7) { "ok" } else { "error" }
```

条件分岐は`if`を使用できます。(switchはまだ実装できてません)
`if`は式で、`{}`内の最後に評価された値を返します。
`{}`内が文だけしかない場合、空タプルを返します。

空タプルは具体的には以下の値です。

```cs
public struct UnitType{}
```


### 関数呼び出し

#### 例
```
fun plus(a, b)
   ret a + b
end

fun main()
   plus(10, 4)
end
```

C#の関数を呼び出す際はスネークケースで関数名を書く必要があり、注意が必要です。
例えば、C#で`HogeFuga`メソッドがあった場合`hoge_fuga()`で呼び出す必要があります。

### 配列
#### 例
```
let array1 = [0, 1, 3]
let array2 = [7, 8; 4] // [7,8,null,null]が生成される
let first = array1[0]
```
配列は`[]`で記述することが出来ます。

要素は全て`System.Object`型として扱われるので、型が全く違う要素を混在させることが出来ます。

`[要素;10]`のようにすると、配列のサイズを指定することができます。
（この例だと10のサイズ）

### オブジェクトのメンバーアクセス

#### 例
```
hoge.fuga = 1212

let aaa = hoge.fuga

hoge.foo(1, 2, 5)
```

オブジェクトのメンバーにアクセスすることが出来ます。
注意する点は、全てスネークケースで記述する必要がある点です。
`HogeFuga`というメンバーがあったとしても、`hoge_fuga`としてアクセスする必要があります。

### アクション

#### 例
```
let action = {|x, y| x + y }
let result = action.invoke([7, 3]) // 10が格納される
```
アクション、いわゆるラムダ式です。

### While文
#### 例
```
let total = 0
let max = 100
let now = 0
while now <= max {
    total = total + now
    now = now + 1
}
```
一般的なwhile文とほとんど同じですが、`()`を省略して書くことが出来ます。

### For文
#### 例
```
let total = 0
// 初期値1で10になるまで、1ずつ増加しながら繰り返す
for i = 1, 10, 1{
    total = total + i
}
```

`for 初期化式, 最大値, 増加値{...}`という構文になってます。

### コルーチン
#### 例
```
fun main() 
    yield
    print("hello")
    yield
    print("world")
    ret 4
end
```
`yield`句を使って処理を中断することが出来ます。
C#側では、`MoveNext()`を使用することで中断した処理を再開出来ます。

### await
#### 例
```cs
public static IEnumerator<int> Wait5()
{
    return Enumerable.Range(1, 5).GetEnumerator();
}
```

```
fun main() 
    let result = wait5().await // 5が格納される
end
```

`await`式を使うことで、`IEnumerator`が終了するまでイテレートして待機します。
`yield`句と同じで、処理を中断して待機します。
`IEnumerator`の最後の値が`await`式の値として評価されます。


### コメント文

#### 例
```
// これはコメント
/*
  これは
  範囲コメント
*/
```
一般的なプログラミング言語のそれと同じです。
