## 字句規約

コメントは`//`で始まり、その行の終わりまでコメントアウトされます。

`/*`と`*/`で囲んだ部分を範囲コメントアウトできます。

## EBNF

```ebnf
program        = {func_definition | macro} ;
func_definition
               = 'fun' , lower_id , variable_list , func_body , 'end' ;
func_body      = {statement} ;
statement      =  
                  break_statement |
                  yield_statement |
                  while_statement |
                  for_statement |
                  ret_statement |
                  assignment |
                  field_assignment |
                  static_field_assignment |
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
               = indexer_op_expr ,  dot_terms , '.' , field_variable , '=' , expr ;
static_field_assignment
               =  class_name , '.' , field_variable , '=' , expr ;
re_assignment_variable  =  variable , '=' , expr ;
re_assignment_indexer  
               = term , indexers , '=' , expr ;
yield_statement
               = 'yield' , expr ; 
break_statement
               = 'break' ;
expr           = if_expr | macro | binary_op_expr ;
if_expr        = 'if' , expr , block , [ 'else' , block ] ;
block          = '{' , {statement} , '}'
binary_op_expr = unary_op_expr , [binary_op , binary_op_expr] ;
unary_op_expr  = { unary_op } , dot_op_expr ;
dot_op_expr    = indexer_op_expr , dot_terms ;
indexer_op_expr
               = static_term | ( term , [indexers] ) ;
dot_terms      = { '.' , field_term , [indexers] } ;
static_term    = class_name , '.' , func_call | field_variable ;
top_level_func_call  
               =  { lower_id , two_colon } , func_call;
field_term     = 'await' | func_call | field_variable ;
term           =
                 '(' , expr , ')' |
                 top_level_func_call | 
                 float_literal | 
                 int_literal | 
                 bool_literal | 
                 char_literal | 
                 string_literal |
                 null_literal |
                 array_literal |
                 action_literal |
                 variable |
                 dict_cons_literal;
action_literal = '{' , action_variable_list , func_body , '}' ;
func_call      = lower_id , [type_param_list] , param_list ;
indexers       = ( '[' , expr , ']' )+ ;
param_list     = '(' , [ expr , { ',' , expr } ] , ')' ;
variable_list  = '(' , [ variable , { ',' , variable } ] , ')' ;
type_param_list 
               = '<' , type_name , { ',' , type_name } , '>' ;
action_variable_list  = '|' , [ variable , { ',' , variable } ] , '|' ;
array_literal  = '[' [ expr , { ',' , expr } ] , [ ';' , int_literal ] , ']' ;
macro          = macro_name , ? トークン文字列群 ? ;
dict_cons_literal 
               = dollar , '{' , [ dict_cons_key_value , { ',' dict_cons_key_value } ] , '}' ;
dict_cons_key_value
               = lower_id , colon , expr ;
variable       = lower_id;
class_name     = upper_id;
type_name      = id , { '.' , id } ;

トークン

float_literal  = int_literal , '.' , int_literal ;
int_literal    = digit ;
bool_literal   = 'true' | 'false' ;
char_literal   = ? 省略 ? ;
string_literal = ? 省略 ? ;
upper_id       = upper_letter , {digit | lower_letter | upper_letter} ; 
lower_id       = lower_letter , {id_char} ;
field_variable = id ;
id             = (lower_letter | upper_letter) , {id_char} ;
id_char        = digit | (lower_letter | upper_letter) | '_' ;
lower_letter   = ? 省略 ?;
upper_letter   = ? 省略 ?;
digit          = '0' | '1' | '2' | '3' | '4' | '5' | '6' | '7' | '8' | '9' ;
binary_op      = '<' | '<=' | '>' | '>=' | '&&' | '||' | '==' | '!=' | '+' | '-' | '*' | '/' | '%' ;
unary_op       = '-' | '!' ;
macro_name     = '#' , {lower_letter | upper_letter | digit} ;
two_colon      = '::' ;
colon          = ':' ;
dollar         = '$' ;
null_literal   = 'null' ;

スキップ

skip           = ' ' | '\n' | '\r' | '\t' ;
```