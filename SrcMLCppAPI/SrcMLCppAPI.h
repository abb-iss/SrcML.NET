#include <utility>
#include <fstream>
using namespace System;
using namespace System::Runtime::InteropServices;
public struct ArchiveAdapter {
    char* encoding;
    char* src_encoding;
    char* revision;
    char* language;
    char* filename;
    char* url;
    char* version;
    char* timestamp;
    char* hash;
    int tabstop;
};
/// <summary>
/// Utility function that trims from the right of a string. For now it's just solving a weird issue with srcML
/// and garbage text ending up at the end of the cstring it returns.
/// </summary>
char* TrimFromEnd(char *s, size_t len){
    for (int i = len - 1; i > 0; --i){
        if (s[i] != '>'){
            s[i] = 0;
        }
        else{
            return s;
        }
    }
    return nullptr;
}
/// <summary>
/// Utility function that is meant to Read a file from beginning to end.
/// </summary>
/// <param name="argv">The name of the file to be read.</param>
std::pair<char*, std::streamoff> ReadFileC(const char* argv){
    std::ifstream is(argv, std::ifstream::binary);
    char * buffer = nullptr;
    std::streamoff length;
    if (is) {
        // get length of file:
        is.seekg(0, is.end);
        length = is.tellg();
        is.seekg(0, is.beg);

        // allocate memory:
        char * buffer = new char[length + 1];

        // read data as a block:
        is.read(buffer, length);

        //add and account for null
        buffer[length] = '\0';
        ++length;

        is.close();

        return std::make_pair(buffer, length);
    }
    else{
        //Should do something here. Either error and quit or exception and handle?
        String^ clistr = gcnew String(argv);
        Console::WriteLine("ERROR, could not open file: {0}", clistr);
        throw std::exception("Couldn't open file");
    }

}