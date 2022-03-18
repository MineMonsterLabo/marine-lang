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

### null値
`null`と書くことで、`null`値を使うことが出来ます。

### 辞書構造
#### Dictionary<string,object>の生成
Marine言語では下記のような書き方でDictionaryを生成できます。

```
//例1
let t = ${}

//例2
let tt = ${
    a : 888,
    b : "aaa"
    c : {|x| x+1}
}
```

---

#### Dicitonaryの要素アクセス(まだ未実装)
※ 未実装なので、[]を使ってインデクサー経由かC#メソッドで利用してください。
```
t.acac = 888
let a = t.acac
```

---

#### Dicitonaryの要素削除

単にDictionaryの削除メソッドを呼び出すだけです。

```
t.remove("a")
```

---

#### 制約
`table.element`のアクセス方法では、
キー名にはスネークケースしか付けれない。
キー名を大文字で扱いたいときは、
DictionaryのC#メソッドなどを使用する。

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
    yield null
    print("hello")
    yield null
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

### 名前空間
名前空間を利用することで、名前が衝突しないように整理することが可能です。
#### Marine言語側での利用

名前空間に内包された関数`::`を使い呼び出すことが可能です。

例えば、`foo`名前空間の`bar`名前空間の`aaa`関数を呼び出す例が以下です。
```
fun foobar() 
    ret foo::bar::aaa() 
end
```
なお現在、相対的に名前空間をアクセスすることはできません。
`foo`名前空間にいる状態で`aaa`関数を呼び出す場合も`foo::bar::aaa()`とフルパスで記述する必要があります。

#### C#側での利用
名前空間への内包はC#側での制御でのみ可能です。
`LoadProgram`メソッドでロードするプログラムがどの名前空間に含まれるか指定できます。
以下は`foo::bar::aaa`関数を配置する例です。

```cs
var lexer = new MarineLang.LexicalAnalysis.LexicalAnalyzer();
var parser = new SyntaxAnalyzer();
var vm = new MarineLang.VirtualMachines.HighLevelVirtualMachine();

var parseResult = parser.Parse(lexer.GetTokens("fun aaa() ret 25 end"));
vm.LoadProgram(new[] { "foo", "bar" }, parseResult.programAst);
```

C#側から名前空間に内包されたメソッドを呼び出す場合、Runメソッドの引数も変更する必要があります。
以下は、`foo::bar::aaa`メソッドをC#側から呼び出す場合です。

```cs
vm.Run<int>(new[] { "foo", "bar" }, "aaa").Eval()
```

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
