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

## [構文の詳細](syntax.md)

## [言語仕様](language_specification.md)