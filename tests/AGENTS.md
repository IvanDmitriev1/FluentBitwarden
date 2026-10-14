# Test project conventions

These test projects keep declared `static` methods in `static` classes. This
is review guidance for test support code only; do not add an analyzer or
another dependency to enforce it.

Static lambdas are fine where they make callbacks allocation-free or explicit.
xUnit `MemberData` static properties are also intentional exceptions because
the test framework discovers data through those properties.
