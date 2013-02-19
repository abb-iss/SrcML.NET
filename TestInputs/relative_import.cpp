#include <iostream>
#include "relative_import.h"

using std::cout;
using std::endl;
using namespace A;

int main() {
  foo();
  return 0;
}

void A::foo() {
  cout << "A::nested::bar is " << nested::bar << endl;
  cout << "executed A::foo()" << endl;
}
