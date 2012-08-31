#include <iostream>

char* MyFunction(int foo) {
    if(foo > 0) {
        return "Hello world!";
    } else {
        return "Goodbye cruel world!";
    }
}

int main(int argc, char* argv[]) {
    std::cout<<MyFunction(42);
    return 0;
}
