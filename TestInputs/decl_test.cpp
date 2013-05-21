#include <iostream>

using std::cout;
using std::endl;
int main() {
  if(int a = 5) {
    cout << "a is " << a << endl;
  } else {
    cout << "that shouldn't have been false";
  }
  cout << "does a exist?" << a << endl;
  return 0;
}
