#include <iostream>

using std::cout;
using std::endl;
int foo(int a);

int main() {
  foo(5);
  return 0;
}

int foo(int a) {
  int a = 6;
  cout << a << endl;
}
