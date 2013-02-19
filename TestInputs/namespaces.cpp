#include "A.h"
#include <iostream>
using std::cout;
using std::endl;

namespace B = A;
typedef B::Foo Foo;

int main() {
  B::Foo test;
  Foo test2;
  test.a = 0;
  test2.a = 1;
  
  cout << "0 + 5 = " << test.Add(5) << endl;
  cout << "1 + 5 = " << test2.Add(5) << endl;
  return 0;
}
