## Kotlin

### Объявления и типы
| Конструкция | Пример | C++ |
|---|---|---|
| `val` / `var` без типа | `val x = 5` | `const auto x = 5;` / `auto` |
| С типом | `val x: Int = 5`, `var s: String` | `const int x = 5;` |
| Объявление без значения | `var x: Int` | `int x;` |
| Простые типы | `Int Long Double Float Boolean Char String Unit` | `int long double float bool char string void` |
| Списки и массивы | `List<T> MutableList<T> ArrayList<T> Array<T> IntArray DoubleArray LongArray BooleanArray CharArray` | `vector<T>` |
| Nullable-типы | `Int?` | знак `?` отбрасывается |

`val` для изменяемых коллекций (`mutableListOf`, `IntArray`, ...) в `const` не превращается.

### Функции
- Блочное тело: `fun f(x: Int): Int { ... }`
- Тело-выражение: `fun f(x: Int) = x * 2` (без указанного типа возврата получается `auto`; такую функцию нельзя вызывать до её определения)
- Параметры по умолчанию: `fun f(x: Int = 1)`
- `List` и `String` в параметрах передаются как `const &`, `MutableList` как `&`
- Рекурсия, прототипы создаются автоматически
- `fun main()`: параметры отбрасываются, в конец добавляется `return 0`
- Код без `main` заворачивается в `int main()`

### Выражения
- Литералы: `42`, `10L`, `3.14`, `1.5f`, `"text"`, `'c'`, `true`, `false`
- Арифметика `+ - * / %`, сравнения `< > <= >= == !=`, логика `&& || !`, унарный минус, скобки
- `x!!` (отбрасывается), `a?.b` (как обычный `.`)
- `if` как выражение: `val m = if (a > b) a else b` (тернарник)
- `when` как выражение (вложенные тернарники)
- Строковые шаблоны: `"a $x b ${expr}"`, в том числе с вложенными кавычками внутри `${...}`
- Лямбды: `{ it * 2 }`, `{ x -> x + 1 }`, `{ a, b -> ... }`, trailing-лямбды
- `x in 1..5`, `x in xs`, `x !in xs`

### Инструкции
- Присваивания: `= += -= *= /= %=`, `i++`, `i--`
- Присваивание по индексу: `xs[i] = v`, `xs[i] += v`
- `if / else if / else`
- `when` как инструкция: с аргументом (`1, 2 ->`, `in 3..9 ->`, `else ->`) и без аргумента
- Циклы:
  - `for (i in a..b)`, `a until b`, `a downTo b`, `... step s`
  - `for (x in xs)`, `for (i in xs.indices)`
  - `while`, `do-while`, `break`, `continue`
  - `repeat(n) { ... }`
- `return`, `throw`, `try / catch / finally`

### Стандартная библиотека
| Область | Что поддержано |
|---|---|
| Вывод и ввод | `println`, `print`, `readLine()`, `readln()` |
| Создание списков | `listOf`, `mutableListOf`, `arrayListOf`, `arrayOf`, `intArrayOf`, `doubleArrayOf`, `longArrayOf`, `booleanArrayOf`, `charArrayOf`, `emptyList<T>()`, `IntArray(n)`, `ArrayList<T>()`, вложенные списки |
| Свойства списков и строк | `.size`, `.length`, `.lastIndex`, `e.message` |
| Доступ | `xs[i]`, `get`, `set`, `first()`, `last()` |
| Изменение | `add`, `add(i, x)`, `addAll`, `removeAt`, `removeLast`, `remove`, `clear`, `sort`, `sortDescending`, `reverse` |
| Запросы | `contains`, `indexOf`, `isEmpty`, `isNotEmpty`, `sorted`, `sortedDescending`, `reversed`, `sum`, `max`, `min`, `average`, `joinToString`, `toList`, `toMutableList`, `count()` |
| Лямбды над списками | `forEach`, `map`, `filter`, `any`, `all`, `none`, `count { }`, `sumOf` |
| Строки | `uppercase`, `lowercase`, `substring`, `startsWith`, `endsWith`, `contains`, `split` |
| Преобразования | `toInt`, `toLong`, `toDouble`, `toString` |
| Математика | `maxOf`, `minOf`, `abs`, `sqrt`, `pow` и другие функции `<cmath>` как есть |
| Исключения | `Exception("...")`, `...Exception(...)`, `...Error(...)` превращаются в `runtime_error(...)` |
