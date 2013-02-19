#include <iostream>

int A = 5;
int main() {
  std::cout << "A (global) is " << A << std::endl;
  int A = 10;
  std::cout << "A (local) is " << A << std::endl;
  std::cout << "::A (global) is " << ::A << std::endl;
  return 0;
}
