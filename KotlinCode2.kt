val LIMIT = 5

// функция с телом-выражением и без указанного типа (в C++ станет auto)
fun square(x: Int) = x * x

// параметр по умолчанию + repeat + шаблон строки
fun greet(name: String, times: Int = 1) {
    repeat(times) {
        println("Hello, $name!")
    }
}

// when как выражение (без subject)
fun grade(score: Int): String = when {
    score >= 90 -> "A"
    score >= 75 -> "B"
    score >= 50 -> "C"
    else -> "F"
}

// when как инструкция (с subject, перечислением и диапазоном)
fun describe(n: Int) {
    when (n) {
        0 -> println("zero")
        1, 2 -> println("small")
        in 3..9 -> println("medium")
        else -> {
            println("large: $n")
        }
    }
}

// рекурсия + if как выражение
fun factorial(n: Int): Long {
    return if (n <= 1) 1 else n * factorial(n - 1)
}

// List<Int> в параметре (const vector<int>&), цикл по коллекции, +=
fun sumEven(xs: List<Int>): Int {
    var s = 0
    for (x in xs) {
        if (x % 2 == 0) s += x
    }
    return s
}

// MutableList<Int> в параметре (vector<int>&)
fun fillSquares(out: MutableList<Int>, n: Int) {
    for (i in 1..n) out.add(square(i))
}

// while + break + continue
fun isPrime(n: Int): Boolean {
    if (n < 2) return false
    var d = 2
    while (d * d <= n) {
        if (n % d == 0) return false
        d++
    }
    return true
}

// исключения
fun safeDivide(a: Int, b: Int): Int {
    if (b == 0) {
        throw IllegalArgumentException("Division by zero")
    }
    return a / b
}

fun main() {
    // ---------- базовые типы и шаблоны строк ----------
    val pi = 3.14
    val big = 10000000000L
    val letter = 'k'
    val ratio: Double = 7 / 2.0
    println("pi=$pi big=$big letter=$letter ratio=$ratio")
    println("square of 7 + 1 = ${square(7) + 1}")

    greet("World", 2)
    greet("Kotlin")

    println("grade(82) = ${grade(82)}")
    describe(0)
    describe(2)
    describe(5)
    describe(42)
    println("10! = ${factorial(10)}")

    // ---------- списки ----------
    val nums = listOf(5, 3, 8, 1, 9, 2)
    val names = listOf("Zoe", "Adam", "Mia")
    println("size=${nums.size} last index=${nums.lastIndex} first=${nums.first()}")
    println("sum=${nums.sum()} max=${nums.max()} min=${nums.min()} avg=${nums.average()}")
    println(nums.joinToString(", "))
    println(nums.sorted().joinToString(" < "))
    println(nums.reversed().joinToString(" "))

    for (i in nums.indices) {
        print("nums[$i]=${nums[i]} ")
    }
    println()

    // функции высшего порядка и лямбды
    val doubled = nums.map { it * 2 }
    val evens = nums.filter { x -> x % 2 == 0 }
    println("doubled: ${doubled.joinToString(", ")}")
    println("evens: ${evens.joinToString(", ")}, sumEven=${sumEven(nums)}")
    println("sum of squares = ${nums.sumOf { it * it }}")
    println("any>8: ${nums.any { it > 8 }}, all>0: ${nums.all { it > 0 }}, count>3: ${nums.count { it > 3 }}")
    names.sorted().forEach { println("name: $it") }

    // принадлежность
    if (3 in nums) println("3 is in nums")
    if (42 !in nums) println("42 is not in nums")
    if (names.contains("Mia")) println("Mia found")
    println("5 in 1..10 -> ${5 in 1..10}")

    // изменяемый список
    val data = mutableListOf(5, 3, 8, 1)
    data.add(10)
    data.add(0, 7)
    data.sort()
    data.removeAt(0)
    data[0] = 99
    data[1] += 100
    println("data = ${data.joinToString(", ")}")

    val squares = mutableListOf<Int>()
    fillSquares(squares, LIMIT)
    println("squares = ${squares.joinToString(", ")}")

    // вложенные списки
    val matrix = listOf(listOf(1, 2, 3), listOf(4, 5, 6))
    var total = 0
    for (row in matrix) {
        for (v in row) {
            total += v
        }
    }
    println("matrix[1][2]=${matrix[1][2]} total=$total")

    // ---------- циклы ----------
    for (i in 1..LIMIT) print("$i ")
    println()
    for (i in 0 until 5) print("$i ")
    println()
    for (i in 10 downTo 1 step 3) print("$i ")
    println()
    for (i in 0..20 step 5) print("$i ")
    println()

    var count = 0
    do {
        count++
    } while (count < 3)
    println("count=$count")

    var found = 0
    for (n in 2..50) {
        if (!isPrime(n)) continue
        found++
        if (found == 10) {
            println("10th prime = $n")
            break
        }
    }

    // ---------- if как выражение ----------
    val a = 17
    val b = 25
    val bigger = if (a > b) a else b
    val parity: String = if (a % 2 == 0) "even" else "odd"
    println("bigger=$bigger a is $parity")

    // ---------- строки ----------
    val text: String = "Kotlin to C++"
    println(text.uppercase())
    println("length=${text.length} sub=${text.substring(0, 6)}")
    if (text.contains("C++")) println("mentions C++")
    val words = text.split(" ")
    println("words: ${words.size}")
    val numberText: String = "42"
    println("parsed + 8 = ${numberText.toInt() + 8}")

    // ---------- исключения ----------
    try {
        println(safeDivide(10, 2))
        println(safeDivide(10, 0))
    } catch (e: Exception) {
        println("Caught: ${e.message}")
    } finally {
        println("done")
    }
}