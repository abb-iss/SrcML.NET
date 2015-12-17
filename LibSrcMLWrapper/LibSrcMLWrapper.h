// LibSrcMLWrapper.h

#pragma once

using namespace System;
using namespace System::Runtime::InteropServices;
#include <fstream>
#include <iostream>
namespace LibSrcMLWrapper {
    //Structs are laid out in specific order; C++ assumes C# will send structs with this particular memory layout
    //If you modify the structure here, you MUST modify it on C#'s end as well. 
    public struct UnitData{
        char* encoding;
        char* revision;
        char* language;
        char* filename;
        char* url;
        char* version;
        char* timestamp;
        char* hash;
        int eol;
        char* buffer;
        int bufferSize;
    };
    public struct SourceData {
        int unitCount;
        int tabstop;
        unsigned long optionSet;
        unsigned long optionEnable;
        unsigned long  optionDisable;
        char** extandlanguage;
        char** prefixandnamespace;
        char** targetanddata;
        char** tokenandtype;
        char* url;
        char* language;
        char* version;
        char* srcEncoding;
        char* xmlEncoding;
        char* outputFile;
        UnitData* units;
    };
    /// <summary>
    /// Utility function that trims from the right of a string. For now it's just solving a weird issue with srcML
    /// and garbage text ending up at the end of the cstring it returns.
    /// </summary>
    inline char* TrimFromEnd(char *s, size_t len){
        for (int i = len - 1; i > 0; --i){
            if (s[i] != '>'){
                s[i] = 0;
            }else{
                return s;
            }
        }
        return nullptr;
    }
    /// <summary>
    /// Utility function that is meant to Read a file from beginning to end.
    /// </summary>
    /// <param name="argv">The name of the file to be read.</param>
    inline std::pair<char*, std::streamoff> ReadFileC(const char* argv){
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
        }else{
            Exception^ error = gcnew Exception(String::Format("ERROR, could not open file: {0}", gcnew String(argv)));
            throw error;
        }

    }
}
