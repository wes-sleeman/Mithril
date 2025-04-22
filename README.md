# Mithril
> Low-level programming without the footguns.

## Overview
Mithril is a mid-level procedural programming language (like C) that aims to make writing bad code
hard. It is case-sensitive, statically typed, block-scoped, immutable, enforces encapsulation, and
interoperates via the C ABI.

## Syntax
A Mithril _program_ is composed of any number (1+) of _source files_. Each Mithril source file
consists of any number (0+) of _definitions_. Definitions within a source file are ordered from
top-to-bottom. Source files within a program are unordered. There are three types of definition:
_value definitions_, _procedure definitions_, and _type definitions_.


```bnf
File	::= Definition Definitions
		  | ε
Definition	::= ValueDef
			  | ProcedureDef
			  | TypeDef
```
{.bnf}

### Value Definitions
Value definitions require a _pattern_ and a _body_, and MAY be preceded by _modifiers_. A body is
either an _expression_ indicated by an equals sign `=` and terminated with a semicolon `;` or it is
a _block_.

```bnf
ValueDef	::= Modifiers 'let' Pattern Body
			  | Modifiers TypeExpr Pattern Body
Body	::= '=' Expr ';'
		  | Block
```
{.bnf}

### Procedure Definitions
Procedure definitions require an _identifier_, a _parameter_ (in the form of a record pattern), and
a body, and MAY be preceded by modifiers.

```bnf
ProcedureDef	::= Modifiers 'let' id RecordPattern Body
				  | Modifiers TypeExpr id RecordPattern Body
```
{.bnf}

### Type Definitions
Type definitions require an identifier and a _type expression_, and MAY be preceded by modifiers.

```bnf
TypeDef	::= Modifiers 'type' id '=' TypeExpr ';'
```
{.bnf}

### Definition Modifiers
Value and procedure definitions may be modified by placing certain keywords before them.
Currently, only encapsulation modifiers are specified:

```bnf
Modifiers	::= Encapsulation
```
{.bnf}

#### Encapsulation
The following encapsulation modifiers are defined: `public`, `internal`, and the empty string.
```bnf
Encapsulation	::= 'public'
				  | 'internal'
				  | ε
```
{.bnf}

### Patterns
A pattern is either a _literal_, an _identifier_, a _record pattern_, or any of the preceding with
a _type tag_. A type tag is a colon `:` followed by a type expression.

```bnf
Pattern	::= PatternItem
		  | PatternItem ':' TypeExpr
PatternItem	::= id
			  | Literal
			  | RecordPattern
```
{.bnf}

#### Identifiers
An identifier is either:
* A maximally-expansive non-empty string of characters not containing whitespace, the equals sign,
the full-stop `.`, the comma `,`, the colon, the semicolon, any bracket character, or any quote character.
* Any non-empty string of non-backtick `` ` `` characters surrounded by single backticks.

Formally, identifiers match the regular expression ``[^\s=.,:;\α]+|`[^`]+` `` where `\α` is any
bracket or quote character (Unicode characters in categories Ps, Pe, Pi, or Pf; e.g. `(`, `]`, `{`,
`"`, `«`, or `」`).

Note that there is no method of escaping backticks; they MUST NOT occur in an identifier.

Additionally identifiers not wrapped in backticks are constrained in that they MUST NOT be a valid
literal as defined below.

The identifiers of public declarations which are also valid identifiers in C MUST NOT be mangled.
Those identifiers of internal or private declarations or those which are not valid C identifiers
MAY be mangled in a manner defined by the implementation.

#### Literals
A literal is either:
1. An integer numeric value in decimal format.
1. A non-integer numeric value in decimal format whose decimal mark is a full-stop.
1. Exactly one character or _escape sequence_ surrounded by single-quotes `'`.
1. A (possibly empty) string of characters or escape sequences surrounded by double-quotes `"`.
1. The verbatim keyword `true`.
1. The verbatim keyword `false`.
1. The verbatim keyword `poison`.

Cases 1 & 2 are _numeric literals_, case 3 is the _character literal_, case 4 is the _string
literal_, cases 5 & 6 are _boolean literals_, and case 7 is the _poison literal_.

Numeric literals MAY be implementation-constrained, but all implementations MUST support at least
the range from -32,768 to 32,767, inclusive. Implementations SHOULD support the range from
-2,147,483,648 to 2,147,483,647, inclusive. Range-bounds notwithstanding, integer numeric literals
MUST match the regex `-?\d+` and non-integer numeric literals MUST match the regex
`-?(\d+\.\d*|\.\d+)`.

An escape sequence is a backslash `\` followed by one further character. Escape sequences MUST be
interpreted according to the following table:

| Sequence | Value |
| -------- | ----- |
| `\"` | U+0022 Quotation Mark |
| `\'` | U+0027 Apostrophe |
| `\b` | U+0008 Backspace |
| `\n` | U+000A New Line |
| `\r` | U+000D Carriage Return |
| `\t` | U+0009 Horizontal Tabulation |
| `\\` | U+005C Reverse Solidus |

Any escape sequence not listed above SHOULD be reported by tooling and MUST be treated as if the
backslash were not present (e.g. `\a` is equivalent to `a`, `\c` is the same as `c`, etc.).

```bnf
Literal	::= numericLiteral
		  | characterLiteral
		  | stringLiteral
		  | booleanLiteral
		  | poisonLiteral
```
{.bnf}

#### Record Patterns
A record pattern is a comma-separated list of record pattern items surrounded by matched
parentheses `(` and `)`. There may be zero, one, or multiple items in the list. The list MAY be
comma-terminated. A record pattern item is a pattern, optionally prefixed by a key and an equals
sign.

```bnf
RecordPattern		::= '(' RecordPatternItems ')'
RecordPatternItems	::= RecordPatternItem ',' RecordPatternItems
					  | RecordPatternItem
					  | ε
RecordPatternItem	::= Pattern
					  | Key '=' Pattern
```
{.bnf}

### Expressions
An expression is a _qualified identifier_, a literal, a _procedure call_, a _record expression_, a
_conditional_, a _map_, or any of the preceding with a type tag.

```bnf
Expr		::= ExprItem
			  | ExprItem ':' TypeExpr
ExprItem	::= QualifiedId
			  | Literal
			  | ProcedureCall
			  | RecordExpr
			  | Conditional
			  | Map
```
{.bnf}

#### Qualified Identifiers
A qualified identifier is a full stop-delimited list of _keys_. A qualified identifier MUST either
contain at least two elements or be a single identifier.

```bnf
QualifiedId	::= id
			  | ExplicitlyQualifiedId

ExplicitlyQualifiedId	::= Key '.' Key
						  | Key '.' ExplicitlyQualifiedId
```
{.bnf}

#### Procedure Calls
A procedure call is an identifier and an argument (in the form of a record expression).

```bnf
ProcedureCall	::= id RecordExpr
```
{.bnf}

#### Record Expressions
A record expression is a comma-separated list of record expression items surrounded by matched
parentheses `(` and `)`. There may be zero, one, or multiple items in the list. The list MAY be
comma-terminated. A record expression item is an expression which is optionally preceded by both a
_key_ and an equals sign `=`. A key may be either an identifier or a literal.

```bnf
RecordExpr		::= '(' RecordExprItems ')'
RecordExprItems	::= RecordExprItem ',' RecordExprItems
				  | RecordExprItem
				  | ε
RecordExprItem	::= Expr
				  | Key '=' Expr
Key	::= id
	  | Literal
```
{.bnf}

#### Conditionals
A conditional is the keyword `if`, a _condition_ (in the form of an expression), a _consequent_ (in
the form of an expression or a block), the keyword `else`, and an _alternative_ (in the form of an
expression terminated by a semicolon or a block). Both the consequent and the alternative are
required in all cases.

```bnf
Conditional	::= 'if' Expr ExprOrBlock 'else' Expr ';'
			  | 'if' Expr ExprOrBlock 'else' Block
ExprOrBlock	::= Expr
			  | Block
```
{.bnf}

#### Maps
A map is the keyword `map`, an _iterator binding_ (in the form of a pattern), the keyword `over`, a
_collection_ (in the form of an expression), and a _transformation_ (in the form of a body).

```bnf
Map	::= 'map' Pattern 'over' Expr Body
```
{.bnf}

### Type Expressions
A type expression is either an identifier, a _type record_, a pointer to an inferred type, or a
pointer to a known type.

```bnf
TypeExpr	::= id
			  | TypeRecord
			  | 'ptr'
			  | TypeExpr 'ptr' 
```
{.bnf}

#### Type Records
A type record is a comma-separated list of type record items surrounded by matched parentheses `(`
and `)`. There may be zero, one, or multiple items in the list. The list MAY Be comma-terminated.
A type record item is a key, an colon `:`, and a type expression.

```bnf
TypeRecord		::= '(' TypeRecordItems ')'
TypeRecordItems	::= TypeRecordItem ',' TypeRecordItems
				  | TypeRecordItem
				  | ε
TypeRecordItem	::= TypeExpr
				  | Key ':' TypeExpr
```
{.bnf}

### Blocks
A block is an ordered collection of blocks and _statements_.

```bnf
Block		::= '{' BlockItems '}'
BlockItems	::= BlockItem BlockItems
			  | ε
BlockItem	::= Block
			  | Statement
```
{.bnf}

#### Statements
A statement is either a control-flow statement, a value definition, or an expression terminated by a semicolon. A
control-flow statement is either a _return statement_ or both the keyword `unreachable` and a
terminating semicolon.

```bnf
Statement	::= Expr ';'
			  | ValueDef
			  | Return
			  | 'unreachable' ';'
```
{.bnf}

##### Return Statements
If statements contain the keyword `return` followed by a _return value_ (in the form of an
expression) and are terminated by a semicolon.

```bnf
Return	::= 'return' Expr ';'
```
{.bnf}

## Semantics
A Mithril program is the union of the _public_ definitions in its source files. For the purpose of
this section, a 'definition' includes only definitions at _top-level_, that is to say outside of
any block.

> It is an ERROR for the set of _public_ and _internal_ definitions within a program to contain
> duplicate identifiers. That is, each public or internal definition MUST have a unique identifier.
{.error}

A Mithril source file contains definitions marked as _public_, definitions marked as _internal_,
and/or definitions without either modifier; unmodified definitions are referred to henceforth as
_private_ definitions. Public definitions MUST be callable by external programs. Internal
definitions MUST be callable by other files within the same program and SHOULD NOT be callable by
external programs. Private definitions MUST NOT be callable by other files within the same program
and SHOULD NOT be callable by external programs.

> It is an ERROR for the set of all definitions within a source file to contain duplicate
> identifiers. That is, symbols defined at top-level must be unique within a file.
{.error}

### Value Definitions
Value definitions bind a pattern to a value by _pattern matching_. Within a block (i.e. not at
top-level) new bindings MUST replace any and all previous bindings for the same identifier.
Bindings are scoped to the context in which they appear; top-level bindings function as described
above, bindings within a block are visible to anything within the same block after the binding,
including within nested blocks. Bindings are _immutable_; that is to say that if a given identifier
defined before a nested block is rebound within that block, anything after that nested block will
continue to see the un-rebound value. For example:

```
int x = 5;
<MARKER A>

{
	int x = 6;
	<MARKER B>

	{
		<MARKER C>
	}
}

<MARKER D>
```

In the above code, at both `<MARKER A>` and `<MARKER D>`, `x` has the value `5`. At `<MARKER B>`
and `<MARKER C>`, however, `x` has the value `6`.

The type of the binding is _inferred_ from the value. If a type expression is present before the
identifier, the value's type is unified with the provided type expression.

> It is an ERROR if a type is provided for the binding and that type does not match the inferred
> type of the value.
{.error}

### Procedure Definitions
A defined procedure's body may use any of the identifiers specified in its parameter. The body of a
procedure MUST NOT be invoked at the point of definition.

Defined procedures are available to be referenced anywhere within the file and, if they are public
or internal, are also available within other files in the program. Public procedures MUST be
callable externally through the C ABI using a calling convention appropriate to the target 
platform.

### Type Definitions
All type definitions MUST be evaluated before any other definitions. Type definitions MUST NOT
refer to themselves (directly or transitively) except through a pointer.

> It is an ERROR if a type definition is recursive.
{.error}

Defined types are available to be referenced anywhere within the file and, if they are public or
internal, are also available within other files in the program. Public types MUST be laid-out in
a C ABI compliant representation appropriate to the target platform.

### Type Inference
Mithril makes heavy use of type inference to both alleviate developer burden and provide
compile-time assurances to the developer that the code as written aligns with their intentions.
All Mithril programs are fully inferable by design; type annotations give extra assurance and
make portions of the code more human-readable. There is one exception requiring note.

Many developers are accustomed to using full type inference systems in the context of
functional programming or in higher-level languages with generics. The polymorphism that these
languages permit is incompatible with the objective of Mithril as a language; while the cleanliness
of code afforded by this is often highly desirable in higher-level, more abstract languages, it is
fundamentally at odds with the more concrete approach taken here. As such:

> Notwithstanding the below, it is an ERROR for the type of a top-level declaration to not be fully
> concretised.
{.error}

This is a trade-off. Some data structures such as linked lists lend themselves well to such
polymorphic types. The advantage to this restriction, however, is it means that the type of all
arguments to a function (and indeed its return type) are known at compile-time and can be laid out
accordingly. In order to loosen the limitation imposed without compromising on the benefits,
Mithril takes advantage of pointers being of uniform size and representation. 

> Pointers to an unknown type are considered concrete in the context of the above error.
{.info}

As Mithril is statically-typed, it becomes impossible to both dereference _and_ use a pointer to an
unknown type. That is, the type of any pointer which is dereferenced to a value where that value is
subsequently used can be inferred based on that usage. Dereferencing any pointer (including those
of known type) and not subsequently using that value is likely an indication that the dereference
was performed unintentionally. As such, the dereferenced value SHOULD be considered a poison value
and MUST be reported to the user.

> It is a WARNING to dereference a pointer and not subsequently use the dereferenced value.
{.warning}

### Pattern Matching
The other major syntactic convenience offered in Mithril is an extensive use of pattern matching.
Pattern matching in Mithril only occurs between patterns and values, never between pairs of either.
The matching of a pattern and value is defined case-wise:
1. An identifier bound previously within the pattern matches the value it was bound to.
1. A discard identifier `_` matches any value with no bindings generated.
1. An identifier not bound within the pattern matches any value and that value is bound to the
identifier.
1. A literal in a pattern matches a value if and only if the literal and the value are the same.
1. A record pattern matches a _record value_ as described below.
1. In all other cases, the pattern matching fails.

As Mithril is statically typed, it is possible that some pattern matching errors are detectable at
compile-time.

> It is an ERROR to match a pattern with a value of an incompatible type.
{.error}

Some other match failures, however, may not be detectable until runtime; specifically, it may not
be possible to know at compile time if a given value of a compatible type will match a literal
pattern.

> It is UNDEFINED BEHAVIOUR if a pattern match fails at runtime.
{.error}

#### Record Matching
A record pattern `p` matches a record value `v` if either:
1. `p` is the empty record pattern `()` and `v` is the empty record value `()`; or
1. `p` contains at least 1 item, `v` contains at least as many items as `p`, and all items in `p`
match _distinct_ items in `v`.

A record pattern item `pi` contains a pattern `pPat` and, optionally, a key `pKey` and occurs in
position `pPos`. A record value item `vi` contains a value `vVal`, and, optionally, a key `vKey`
and occurs in position `vPos`. `pi` and `vi` match if `pPat` and `vVal` match and either:
* `pKey` is the same as `vKey`; or
* `pKey` is not defined and `pPos` is the same as `vPos`.

To demonstrate by example, consider the following record patterns in light of the record value
`(3, b = (), c = 5, d = 7)`:
* `(a, c = 5, b = ())` matches with `a` being bound to `3`.
* `(3, t, d = 7)` matches with `t` bound to `()`.
* `(3)` matches with no bindings.
* `(3, (), 5, 7)` matches with no bindings.
* `(a, b, 5, c)` matches with `a` bound to `3`, `b` bound to `()`, and `c` bound to `7`.
* `(d = t)` matches with `t` bound to `7`.
* `(a = t)` does not match as no item in the value has key `a`.
* `()` does not match as the value contains more than zero elements and the pattern does not.
* `(5)` does not match as the item in position 0 of the value is `3` and no key was provided.
* `_`, `(c = 5)`, and `(_, _, 5)` _do_, however, match with no bindings.

### Expressions
An expression, when evaluated, generates a value. An expression with a type tag is valid if and
only if the expression's inferred type unifies with the type it is tagged with. Otherwise, an
expression has semantics based on its kind as described in the sections below.

#### Qualified Identifiers
A qualified identifier may be any of the following:
* A previously bound identifier
* A record followed by a dot and a key.
* Inductively, a qualified identifier followed by a dot and a key.

> It is an ERROR to use an unbound identifier in an expression.
{.error}

> It is an ERROR to qualify a literal.
{.error}

In the case that a qualified identifier is a bound identifier, the expression evaluates to the
value the identifier is bound to. If the identifier is a record:
* If the key is a key in the record, the value corresponding to that key is returned.
* If the key is a literal non-negative integer `n` and `n` is less than the number of items in the
record, the value at (zero-indexed) position `n` in the record.
* If the key is a literal negative integer `n` and `n` is of lesser or equal magnitude to the
number of items in the record `c`, the value at (zero-indexed) position `n + c`
* In all other cases, the key is invalid.

> It is an ERROR to qualify a record with an invalid key.
{.error}

In the inductive case, the left-most segment of the identifier is evaluated, then the keys qualify
the value one-at-a-time from left to right as above. `a.b.c.d` finds the value at `d` in the record
value at `c` in the record value at `b` in the record value bound to identifier `a`.
`(((1))).0.0.0` evaluates to `1`, `(((1))).0.0` evaluates to `(1)`, and so on.

#### Literals
Numeric literals are evaluated in decimal. Character literals take the value of their character or
the codepoint defined in the escape sequences table above if appropriate. Public character literals
MUST be encoded in a manner consistent with the target machine. String literals are stored in a
manner defined by the implementation, but public strings MUST be stored as C ABI compliant
character array pointers using the same representation of a character as character literals.
Public boolean literals MUST be stored in a C ABI compliant manner. Poison literals take any value
convenient for the implementation.

#### Procedure Calls
The procedure corresponding to the call's identifier is looked up. The argument of the call is
pattern matched with the parameter of the definition, and the definition's body is executed with
the available bindings at the level of the definition augmented by the results of the pattern
match.

> It is an ERROR to call a procedure with an argument that doesn't match its parameter.
{.error}

#### Record Expressions
Record expressions evaluate to record values as follows:
* A record expression with no items `()` evaluates to a record value with no items `()`.
* A record expression with items evaluates to a record value with items transformed as below.

A record expression item with expression `eExpr` and, optionally, key `eKey` at (zero-indexed)
position `ePos` evaluates to a record value item `v` as follows:
* The position of `v` is `ePos`.
* If `eKey` is defined, the key of `v` is `eKey`.
* If `eKey` is not defined, the key of `v` is likewise undefined.
* The value of `v` is the result of evaluating `eExpr`.

#### Conditionals
A conditional evaluates depending on its condition. The condition is first evaluated to a boolean
value.

> It is an ERROR if the condition of a conditional is not a boolean value.
{.error}

> It is an INFORMATIONAL DIAGNOSTIC if the condition is reducible to a single boolean value at
> compile-time.
{.info}

If the condition is evaluated to the boolean true value, the value of the expression is the same as
the value of the consequent. If the condition is evaluated to the boolean false value, the value of
the expression is the same as the value of the alternative.

> Unless the condition is reducible to a single boolean value at compile-time, it is an ERROR if
> the types of the consequent and the alternative do not unify.
{.error}

#### Maps
A map applies a transformation to each element of a record value and collects the results into a
new record. For each item in the record value at (zero-indexed) position `p` with key (if defined)
`k` and value `v`:
1. The iterator pattern is matched against `(key = k, value = v, index = p)` if the key is defined,
otherwise it is matched against `(key = (), value = v, index = p)`.
1. The body is evaluated with the new bindings from the pattern match.
1. Match the resulting value `r` with `(key = a, value = b)`; if it fails then `a` is `k` and `b`
is `r`.

The map evaluates to a new record where the item at each position in the original record value is
replaced with a new one at the same position with key `a` and value `b` from the above steps.

### Type Expressions
A type expression is either the identifier of a known type, a type record, a pointer, or a pointer
to another type expression.

If a type expression is the identifier of a known type, it evaluates to that known type. All
implementations MUST define the following _base types_:
`int`
`decimal`
`bool`
`char`
`string`

Implementations MAY define additional base types. Type records evaluate to a type that preserves
the order order of their items. Each item's key is preserved verbatim and the associated type
expression corresponds to the required type of the value correlated to that key. If no key is
specified, the type corresponds to the required type at that position.

### Blocks
Blocks evaluate to whatever is _returned_ by a contained `return` statement. There may be multiple
so long as the types of all possibly returned values unify.

> It is an ERROR for a block to have return statements with non-unifying types.
{.error}

If no return statements are encountered in the execution of the block, it returns `poison`.

> It is a WARNING if:
> 1. There are return statements in the block.
> 1. Any return statement within the block returns something other than the poison literal.
> 1. There is a possible execution path through the block that does not encounter a return
> statement.
{.warning}

Blocks are executed sequentially and the values bound in their value definitions may only be
referenced _after_ the value definition occurs. For statements which are semicolon-terminated
expressions, the expression is evaluated and the resulting value is discarded. The special
statement `unreachable;` triggers undefined behaviour if executed. Tooling MAY assume that such a
statement cannot be reached by any execution path and MUST suppress any diagnostics generated by
semantic evaluation of any code whose execution path is dominated by the statement.
