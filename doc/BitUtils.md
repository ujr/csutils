
BitUtils
========

The BitUtils class provides bitwise operations.

The PopulationCount of *x* or POP(x) is the number of one bits in 
*x*. For example, POP(5) = 2 because 5 (decimal) is 101 (binary).

The LeadingZeroCount or NLZ(x) is the number of zero bits before 
the first one bit. Similarly, TrailingZeroCount or NTZ(x) is the 
number of zero bits after the last one bit. For example, NTZ(8) = 3 
because 8 (decimal) is 1000 (binary). NLZ assumes a fixed bit width 
of 32 or 64 based on the argument type.

An integer *x* in binary representation is a power of two 
if POP(x) = 1 (otherwise it's a sum of powers of two). 
Utility methods exist to test if their argument is a power of two, 
and to round its argument up (ceiling) or down (floor) to 
the nearest power of two.

The methods in the BitUtils class make heavy use of bit shifting. 
Beware that `x>>n` in C# is arithmethic shift (with sign extension) 
if `x` is signed, but logical shift (fill with zero bits) if `x` is 
unsigned. This differs from Java, which always does arithmetic right 
shift but provides an extra unsigned right shift operator `>>>`. 
While at it: both Java and C# shift by `n&31` (if x is 32 bits) or 
by `n&63` (if x is 64 bits); this differs from C, where shifting 
by more than the word size is explicitly undefined.
