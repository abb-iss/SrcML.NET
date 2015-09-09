// This is the main DLL file.

#include "stdafx.h"
#include "srcml.h"
#include "SrcMLCppAPI.h"
#include <iostream>
using namespace System;
using namespace System::Runtime::InteropServices;
void SetMetaData(srcml_unit* unit, SourceData* archiveData){
    if (archiveData->src_encoding != ""){
        srcml_set_src_encoding(archiveData->src_encoding);
    }

    if (archiveData->language != ""){
        String^ clistr = gcnew String(archiveData->language);
        Console::WriteLine("AD'S LANGUAGE IS: {0}", clistr);
        srcml_unit_set_language(unit, archiveData->language);
        srcml_set_language(archiveData->language);
    }

    if (archiveData->tabstop != 0){
        srcml_set_tabstop(archiveData->tabstop);
    }

    if (archiveData->url != ""){
        srcml_set_url(archiveData->url);
    }

    if (archiveData->timestamp != ""){
        srcml_set_timestamp(archiveData->timestamp);
    }

    if (archiveData->hash != ""){
        srcml_set_hash(archiveData->hash);
    }

    if (archiveData->version != ""){
        srcml_set_version(archiveData->version);
    }

    if (archiveData->encoding != ""){
        srcml_set_xml_encoding(archiveData->encoding);
    }

}
extern"C"{
    /// <summary>
    /// This creates an archive from a list of files and saves to a file
    /// </summary>
    ///<param name="argv">List of files to be read</param>
    ///<param name="argc">Number of arguments in argv</param>
    ///<param name="outputFile">File to output to</param>
    __declspec(dllexport) int SrcmlCreateArchiveFtF(SourceData** sd, int argc, const char* outputFile) {
        int i;
        struct srcml_archive* archive;
        struct srcml_unit* unit;		

        /*create a new srcml archive structure */
        archive = srcml_archive_create();

        /*open a srcML archive for output */
        srcml_archive_write_open_filename(archive, outputFile, 0);
        /* add all the files to the archive */
        for (int i = 0; i < argc; ++i){
            for (int k = 0; k < sd[i]->buffercount; ++k) {
                unit = srcml_unit_create(archive);

                String^ clistr = gcnew String(sd[i]->encoding);
                String^ clistr2 = gcnew String(sd[i]->filename[k]);
                Console::WriteLine("AD'S ENCODING IS: {0} and file name is: {1}", clistr, clistr2);

                SetMetaData(unit, sd[i]);

                srcml_unit_set_filename(unit, sd[i]->filename[k]);

                /*Translate to srcml and append to the archive */
                srcml_unit_parse_filename(unit, sd[i]->filename[k]);

                /*Translate to srcml and append to the archive */
                srcml_write_unit(archive, unit);

                srcml_unit_free(unit);
            }
        }


        /*close the srcML archive */
        srcml_archive_close(archive);

        /*free the srcML archive data */
        srcml_archive_free(archive);

        //Return 0 to say it worked-- need to do error checking still for when srcml returns an issue.
        return 0;
    }
    /// <summary>
    /// This creates an archive from a buffer and saves to a file 
    /// </summary>
    ///<param name="argv">List of files to be read</param>
    ///<param name="argc">Number of arguments in argv</param>
    ///<param name="outputFile">File to output to</param>
    __declspec(dllexport) int SrcmlCreateArchiveMtF(SourceData** sd, int argc, const char* outputFile) {
        for (int i = 0; i < argc; ++i){
            struct srcml_archive* archive;
            /* create a new srcml archive structure */
            archive = srcml_archive_create();
            /* open a srcML archive for output */
            int numRead = 0;
            srcml_archive_write_open_filename(archive, "test.xml", 0);
            for (int j = 0; j < sd[i]->buffercount; ++j){
                struct srcml_unit* unit;
                /* add all the files to the archive */

                unit = srcml_unit_create(archive);
                SetMetaData(unit, sd[i]);
                srcml_unit_set_filename(unit, sd[i]->filename[j]);

                /*Parse*/
                int error = srcml_unit_parse_memory(unit, sd[i]->buffer[j], sd[i]->buffersize[j]);
                if (error){
                    //Handle error
                }

                /* Translate to srcml and append to the archive */
                srcml_write_unit(archive, unit);
                srcml_unit_free(unit);

            }

            /* close the srcML archive */
            srcml_archive_close(archive);

            /* free the srcML archive data */
            srcml_archive_free(archive);
        }

        //Return 0 to say it worked-- need to do error checking still for when srcml returns an issue.
        return 0;
    }
    /// <summary>
    /// This creates an archive from a file and returns the resulting srcML in a buffer
    /// </summary>
    ///<param name="argv">List of files to be read</param>
    ///<param name="argc">Number of arguments in argv</param>
    __declspec(dllexport) char* SrcmlCreateArchiveFtM(SourceData** sd, int argc) {
        struct srcml_archive* archive;
        struct srcml_unit* unit;
        char * s;
        size_t size;

        /* create a new srcml archive structure */
        archive = srcml_archive_create();

        /* open a srcML archive for output */
        srcml_archive_write_open_memory(archive, &s, &size);

        /* add all the files to the archive */
        for (int i = 0; i < argc; ++i) {
            for (int k = 0; k < sd[i]->buffercount; ++k){
                unit = srcml_unit_create(archive);

                SetMetaData(unit, sd[i]);

                srcml_unit_set_filename(unit, sd[i]->filename[k]);
                //Read file into pair of c-string and size of the file. TODO: Error check
                std::pair<char*, std::streamoff> bufferPair = ReadFileC(sd[i]->filename[k]);

                srcml_unit_parse_memory(unit, bufferPair.first, bufferPair.second);

                /* Translate to srcml and append to the archive */
                srcml_write_unit(archive, unit);

                srcml_unit_free(unit);

                delete[] bufferPair.first;
            }
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
    /// This creates an archive from a buffer and returns the resulting srcML in a buffer
    /// </summary>
    ///<param name="argv">List of files to be read</param>
    ///<param name="argc">Number of arguments in argv</param>
    __declspec(dllexport) char* SrcmlCreateArchiveMtM(SourceData** sd, int argc) {
        struct srcml_archive* archive;
        struct srcml_unit* unit;
        char * s;
        size_t size;
        for (int i = 0; i < argc; ++i){
            /* create a new srcml archive structure */
            archive = srcml_archive_create();

            /* open a srcML archive for output */
            srcml_archive_write_open_memory(archive, &s, &size);
            for (int k = 0; k < sd[i]->buffercount; ++k){
                unit = srcml_unit_create(archive);
                SetMetaData(unit, sd[i]);
                srcml_unit_set_filename(unit, sd[i]->filename[k]);
                /*Parse*/
                srcml_unit_parse_memory(unit, sd[i]->buffer[k], sd[i]->buffersize[k]);

                /* Translate to srcml and append to the archive */
                srcml_write_unit(archive, unit);
                srcml_unit_free(unit);
            }
            /* close the srcML archive */
            srcml_archive_close(archive);

            /* free the srcML archive data */
            srcml_archive_free(archive);

            /*Trim any garbage data from the end of the string. TODO: Error check*/
            TrimFromEnd(s, size);
        }
        return s;
    }
}