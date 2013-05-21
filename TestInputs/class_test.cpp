#include "class_test.h"

int TestClass::set_x(int val) {
  x = val;
  return x;
}

int TestClass::set_y(int y) {
  this->y = y;
  return this->y;
}
