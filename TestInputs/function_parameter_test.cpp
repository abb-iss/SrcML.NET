#include <iostream>

int main(int argc, char **argv){// parameter declaration(argc,argv)
  int i = 0;
  while(i < argc) {// parameter use (argc)
    std::cout << argv[i] << std::endl; // parameter use (argv)
    i++;
  }
  return 0;
}
