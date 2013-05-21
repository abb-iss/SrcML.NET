#include "global_conflict_test.h"
#include <iostream>

using std::cout;
using std::endl;

int X = 10;
int main() {
  A a;
  cout << "X is " << a.GetX() << endl;
  cout << "global X is " << X << endl;
}

int A::GetX() {
  return X;
}
