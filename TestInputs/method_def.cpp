#include <iostream>

class MyClass {
public:
    MyClass();
    MyClass(int);
    char* foo(int);
private:
    int number;
    int GetNumber() { return number;}
};

MyClass::MyClass() {
    number = 17;
}

MyClass::MyClass(int num) {
    number = num;
}

char* MyClass::foo(int bar) {
    if(bar > GetNumber()) {
        return "Hello, world!";
    } else {
        return "Goodbye cruel world!";
    }
}

int main(int argc, char* argv[]) {
    MyClass mc;
    std::cout<<mc.foo(5)<<std::endl;
    MyClass* mc2 = new MyClass(0);
    std::cout<<mc2->foo(5)<<std::endl;
    return 0;
}
