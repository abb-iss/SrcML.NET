#include <stdio.h>
int Foo(char* bar) {
    MyObject* mo = new MyObject();
    printf("%d", SomeMethodCall(mo->ToString()));
}
