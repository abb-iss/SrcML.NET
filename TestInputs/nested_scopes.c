int myVar = 0;
printf("hello world %d", myVar);

int main(int argc, char* argv[]) {
    int myVar = 42;
    int result;
    result = CallSomeMethod(myVar);
    if(result > 0){
        int myVar = 17;
        printf("%d %s", myVar, argc);
    }
	return 0;
}
