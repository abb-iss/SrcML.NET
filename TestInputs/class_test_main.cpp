#include "class_test.h"
#include <iostream>

int main() {
  TestClass test = TestClass();
  TestClass *test_pointer = new TestClass();
  test.z = 5;
  test_pointer->z = 5;
  std::cout << "test.z = " << test.z << std::endl;
  std::cout << "test_pointer->z = " << test_pointer->z << std::endl;
  return 0;
}
