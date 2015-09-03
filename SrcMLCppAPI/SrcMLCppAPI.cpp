// This is the main DLL file.

#include "stdafx.h"
#include "srcml.h"
#include "SrcMLCppAPI.h"

using namespace System;
using namespace System::Runtime::InteropServices;

extern"C"{
    /// <summary>
    /// This creates an archive from a list of files
    /// </summary>
    ///<param name="argv">List of files to be read</param>
    ///<param name="argc">Number of arguments in argv</param>
    ///<param name="outputFile">File to output to</param>
    __declspec(dllexport) int SrcmlCreateArchiveFtF(char** argv, int argc, const char* outputFile, ArchiveAdapter* ad){
        int i;
        struct srcml_archive* archive;
        struct srcml_unit* unit;
        String^ clistr = gcnew String(ad->encoding);
        String^ clistr2 = gcnew String(ad->filename);
        Console::WriteLine("AD'S ENCODING IS: {0} and file name is: {1}", clistr, clistr2);
        /* create a new srcml archive structure */
        archive = srcml_archive_create();

        /* open a srcML archive for output */
        srcml_archive_write_open_filename(archive, outputFile, 0);

        /* add all the files to the archive */
        for (i = 0; i < argc; ++i) {
            unit = srcml_unit_create(archive);

            srcml_unit_set_filename(unit, argv[i]);

            /* Translate to srcml and append to the archive */
            srcml_unit_parse_filename(unit, argv[i]);

            /* Translate to srcml and append to the archive */
            srcml_write_unit(archive, unit);

            srcml_unit_free(unit);
        }

        /* close the srcML archive */
        srcml_archive_close(archive);

        /* free the srcML archive data */
        srcml_archive_free(archive);

        //Return 0 to say it worked-- need to do error checking still for when srcml returns an issue.
        return 0;
    }
    __declspec(dllexport) int SrcmlCreateArchiveMtF(char* argv, int argc, const char* outputFile, ArchiveAdapter* ad){
        int i;
        struct srcml_archive* archive;
        struct srcml_unit* unit;

        /* create a new srcml archive structure */
        archive = srcml_archive_create();

        /* open a srcML archive for output */
        srcml_archive_write_open_filename(archive, outputFile, 0);

        /* add all the files to the archive */
        unit = srcml_unit_create(archive);

        srcml_unit_set_filename(unit, "input.cpp");

        /*Set language*/
        srcml_unit_set_language(unit, SRCML_LANGUAGE_CSHARP);

        /*Parse*/
        srcml_unit_parse_memory(unit, argv, argc);

        /* Translate to srcml and append to the archive */
        srcml_write_unit(archive, unit);
        srcml_unit_free(unit);

        /* close the srcML archive */
        srcml_archive_close(archive);

        /* free the srcML archive data */
        srcml_archive_free(archive);

        //Return 0 to say it worked-- need to do error checking still for when srcml returns an issue.
        return 0;
    }
    /// <summary>
    /// This creates an archive from a file and returns the resulting srcML as a string
    /// </summary>
    ///<param name="argv">List of files to be read</param>
    ///<param name="argc">Number of arguments in argv</param>
    __declspec(dllexport) char* SrcmlCreateArchiveFtM(char** argv, int argc, ArchiveAdapter* ad){
        int i;
        struct srcml_archive* archive;
        struct srcml_unit* unit;
        char * s;
        size_t size;

        /* create a new srcml archive structure */
        archive = srcml_archive_create();

        /* open a srcML archive for output */
        srcml_archive_write_open_memory(archive, &s, &size);

        /* add all the files to the archive */
        for (i = 0; i < argc; ++i) {

            unit = srcml_unit_create(archive);
            
            //Read file into pair of c-string and size of the file. TODO: Error check
            std::pair<char*, std::streamoff> bufferPair = ReadFileC(argv[i]);

            srcml_unit_set_language(unit, srcml_archive_check_extension(archive, argv[i]));

            srcml_unit_parse_memory(unit, bufferPair.first, bufferPair.second);

            /* Translate to srcml and append to the archive */
            srcml_write_unit(archive, unit);

            srcml_unit_free(unit);

            delete[] bufferPair.first;
        }

        /* close the srcML archive */
        srcml_archive_close(archive);

        /* free the srcML archive data */
        srcml_archive_free(archive);

        /*Trim any garbage data from the end of the string. TODO: Error check*/
        TrimFromEnd(s, size);
        return s;
    }

    /// <summary>
    /// This creates an archive from a buffer and returns the resulting srcML as a string
    /// </summary>
    ///<param name="argv">List of files to be read</param>
    ///<param name="argc">Number of arguments in argv</param>
    __declspec(dllexport) char* SrcmlCreateArchiveMtM(char* argv, int argc, ArchiveAdapter* ad){
        int i;
        struct srcml_archive* archive;
        struct srcml_unit* unit;
        char * s;
        size_t size;

        /* create a new srcml archive structure */
        archive = srcml_archive_create();

        /* open a srcML archive for output */
        srcml_archive_write_open_memory(archive, &s, &size);
        unit = srcml_unit_create(archive);

        /*Set language*/
        srcml_unit_set_language(unit, SRCML_LANGUAGE_CSHARP);

        /*Parse*/
        srcml_unit_parse_memory(unit, argv, argc);

        /* Translate to srcml and append to the archive */
        srcml_write_unit(archive, unit);
        srcml_unit_free(unit);

        /* close the srcML archive */
        srcml_archive_close(archive);

        /* free the srcML archive data */
        srcml_archive_free(archive);

        /*Trim any garbage data from the end of the string. TODO: Error check*/
        TrimFromEnd(s, size);
        return s;
    }
}