#include <iostream>
int main() {
  int x = 5;// variable declaration (x)
  int y = x + 2;// variable declaration (y), variable use (x)
  
  y = y + x;// variable use (x,y)
  std::cout << "x = " << x << std::endl;
  std::cout << "y = " << y << std::endl;
  return x;// variable use (y)
}
