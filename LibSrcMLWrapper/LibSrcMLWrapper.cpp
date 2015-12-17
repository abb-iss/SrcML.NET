// This is the main DLL file.
#include "stdafx.h"
#include "LibSrcMLWrapper.h"
using namespace System;
using namespace System::Runtime::InteropServices;

extern"C"{
    
    void SetArchiveData(srcml_archive* archive, LibSrcMLWrapper::SourceData* clientArchive){
        if (clientArchive->srcEncoding){
            srcml_archive_set_src_encoding(archive, clientArchive->srcEncoding);
        }
        if (clientArchive->xmlEncoding){
            srcml_archive_set_xml_encoding(archive, clientArchive->xmlEncoding);
        }
        if (clientArchive->language){
            srcml_archive_set_language(archive, clientArchive->language);
        }
        if (clientArchive->url){
            srcml_archive_set_url(archive, clientArchive->url);
        }
        if (clientArchive->version){
            srcml_archive_set_version(archive, clientArchive->version);
        }
        if (clientArchive->optionSet){
            srcml_archive_set_options(archive, clientArchive->optionSet);
        }
        if (clientArchive->optionEnable){
            srcml_archive_enable_option(archive, clientArchive->optionEnable);
        }
        if (clientArchive->optionDisable){
            srcml_archive_disable_option(archive, clientArchive->optionDisable);
        }
        if (clientArchive->tabstop){
            srcml_archive_set_tabstop(archive, clientArchive->tabstop);
        }
        if (clientArchive->extandlanguage){
            srcml_archive_register_file_extension(archive, clientArchive->extandlanguage[0], clientArchive->extandlanguage[1]);
        }
        if (clientArchive->prefixandnamespace){
            srcml_archive_register_namespace(archive, clientArchive->prefixandnamespace[0], clientArchive->prefixandnamespace[1]);
        }
        if (clientArchive->targetanddata){
            srcml_archive_set_processing_instruction(archive, clientArchive->targetanddata[0], clientArchive->targetanddata[1]);
        }
        if (clientArchive->tokenandtype){
            srcml_archive_register_macro(archive, clientArchive->tokenandtype[0], clientArchive->tokenandtype[1]);
        }
    }
    
    void SetUnitData(srcml_unit* unit, LibSrcMLWrapper::UnitData* clientUnit){
        if (clientUnit->language){
            srcml_unit_set_language(unit, clientUnit->language);
        }
        if (clientUnit->encoding){
            srcml_unit_set_src_encoding(unit, clientUnit->encoding);
        }
        if (clientUnit->url){
            srcml_unit_set_url(unit, clientUnit->url);
        }
        if (clientUnit->version){
            srcml_unit_set_version(unit, clientUnit->version);
        }
        if (clientUnit->timestamp){
            srcml_unit_set_timestamp(unit, clientUnit->timestamp);
        }
        if (clientUnit->hash){
            srcml_unit_set_hash(unit, clientUnit->hash);
        }
        if (clientUnit->eol){
            srcml_unit_unparse_set_eol(unit, clientUnit->eol);
        }
    }
    /// <summary>
    /// This creates an archive from a list of files and saves to a file
    /// </summary>
    ///<param name="argv">List of files to be read</param>
    ///<param name="argc">Number of arguments in argv</param>
    ///<param name="outputFile">File to output to</param>
    __declspec(dllexport) int SrcmlCreateArchiveFtF(LibSrcMLWrapper::SourceData** clientArchives, int argc) {
        struct srcml_archive* archive;
        struct srcml_unit* unit;
        int srcmlreturncode = -1;
        for (int i = 0; i < argc; ++i){
            /*create a new srcml archive structure */
            archive = srcml_archive_create();
            SetArchiveData(archive, clientArchives[i]);

            std::string filename(clientArchives[i]->outputFile);
            filename += std::to_string(i) + ".cpp.xml";

            /*open a srcML archive for output */
            srcml_archive_write_open_filename(archive, filename.c_str(), 0);

            /* add all the files to the archive */
            for (int k = 0; k < clientArchives[i]->unitCount; ++k) {

                unit = srcml_unit_create(archive);

                /*Set all srcML options provided through clientArchives*/
                SetUnitData(unit, &clientArchives[i]->units[k]);

                /*Set filename for unit*/
                srcml_unit_set_filename(unit, clientArchives[i]->units[k].filename);

                /*Translate to srcml and append to the archive */
                srcmlreturncode = srcml_unit_parse_filename(unit, clientArchives[i]->units[k].filename);

                /*Translate to srcml and append to the archive */
                srcml_write_unit(archive, unit);

                srcml_unit_free(unit);
                if (srcmlreturncode){
                    srcml_archive_close(archive);
                    srcml_archive_free(archive);
                    Exception^ error = gcnew Exception(String::Format("could not parse file {0}. SrcML returned with status {1}", gcnew String(clientArchives[i]->units[k].filename), srcmlreturncode));
                    throw error;
                }
            }
            /*close the srcML archive */
            srcml_archive_close(archive);

            /*free the srcML archive data */
            srcml_archive_free(archive);
        }
        return srcmlreturncode;
    }
    /// <summary>
    /// This creates an archive from a buffer and saves to a file 
    /// </summary>
    ///<param name="argv">List of files to be read</param>
    ///<param name="argc">Number of arguments in argv</param>
    ///<param name="outputFile">File to output to</param>
    __declspec(dllexport) int SrcmlCreateArchiveMtF(LibSrcMLWrapper::SourceData** clientArchives, int argc) {
        struct srcml_archive* archive;
        struct srcml_unit* unit;
        int srcmlreturncode = -1;
        for (int i = 0; i < argc; ++i){
            /* create a new srcml archive structure */
            archive = srcml_archive_create();
            SetArchiveData(archive, clientArchives[i]);

            /* open a srcML archive for output */
            std::string filename(clientArchives[i]->outputFile);
            filename += std::to_string(i) + ".cpp.xml";

            /*open a srcML archive for output */
            srcml_archive_write_open_filename(archive, filename.c_str(), 0);

            for (int k = 0; k < clientArchives[i]->unitCount; ++k){

                /* add all the files to the archive */

                unit = srcml_unit_create(archive);

                /*Set all srcML options provided through clientArchives*/
                SetUnitData(unit, &clientArchives[i]->units[k]);

                /*Set filename for unit*/
                srcml_unit_set_filename(unit, clientArchives[i]->units[k].filename);

                /*Parse*/
                srcmlreturncode = srcml_unit_parse_memory(unit, clientArchives[i]->units[k].buffer, clientArchives[i]->units[k].bufferSize);

                /* Translate to srcml and append to the archive */
                srcml_write_unit(archive, unit);
                srcml_unit_free(unit);
                if (srcmlreturncode){
                    srcml_archive_close(archive);
                    srcml_archive_free(archive);
                    Exception^ error = gcnew Exception(String::Format("could not parse file {0}. SrcML returned with status {1}", gcnew String(clientArchives[i]->units[k].filename), srcmlreturncode));
                    throw error;
                }
            }

            /* close the srcML archive */
            srcml_archive_close(archive);

            /* free the srcML archive data */
            srcml_archive_free(archive);
        }
        return srcmlreturncode;
    }
    /// <summary>
    /// This creates an archive from a file and returns the resulting srcML in a buffer
    /// </summary>
    ///<param name="argv">List of files to be read</param>
    ///<param name="argc">Number of arguments in argv</param>
    __declspec(dllexport) char** SrcmlCreateArchiveFtM(LibSrcMLWrapper::SourceData** clientArchives, int argc) {
        char** pp = new char*[argc];
        size_t size;
        int srcmlreturncode = 0;
        /* add all the files to the archive */
        for (int i = 0; i < argc; ++i) {
            /* create a new srcml archive structure */
            struct srcml_archive* archive;
            archive = srcml_archive_create();
            SetArchiveData(archive, clientArchives[i]);

            /* open a srcML archive for output */
            srcml_archive_write_open_memory(archive, &pp[i], &size);
            for (int k = 0; k < clientArchives[i]->unitCount; ++k){
                struct srcml_unit* unit;
                unit = srcml_unit_create(archive);

                /*Set all srcML options provided through clientArchives*/
                SetUnitData(unit, &clientArchives[i]->units[k]);

                /*Set filename for unit*/
                srcml_unit_set_filename(unit, clientArchives[i]->units[k].filename);
                std::pair<char*, std::streamoff> bufferPair;
                try{
                    //Read file into pair of c-string and size of the file. TODO: Error check
                    bufferPair = LibSrcMLWrapper::ReadFileC(clientArchives[i]->units[k].filename);

                }
                catch (System::Exception^ e){
                    throw e; //pass exception along to API
                }
                /*Parse memory; bufferpair.first is the c-string from the read. bufferpair.second is the count of characters*/
                srcmlreturncode = srcml_unit_parse_memory(unit, bufferPair.first, bufferPair.second);

                /* Translate to srcml and append to the archive */
                srcml_write_unit(archive, unit);
                srcml_unit_free(unit);

                delete[] bufferPair.first;

                if (srcmlreturncode){
                    srcml_archive_close(archive);
                    srcml_archive_free(archive);
                    Exception^ error = gcnew Exception(String::Format("could not parse file {0}. SrcML returned with status {1}", gcnew String(clientArchives[i]->units[k].filename), srcmlreturncode));
                    throw error;
                }
            }
            /* close the srcML archive */
            srcml_archive_close(archive);

            /* free the srcML archive data */
            srcml_archive_free(archive);

            /*Trim any garbage data from the end of the string. TODO: Error check*/
            LibSrcMLWrapper::TrimFromEnd(pp[i], size);
        }
        return pp;
    }

    /// <summary>
    /// This creates an archive from a buffer and returns the resulting srcML in a buffer
    /// </summary>
    ///<param name="argv">List of files to be read</param>
    ///<param name="argc">Number of arguments in argv</param>
    __declspec(dllexport) char** SrcmlCreateArchiveMtM(LibSrcMLWrapper::SourceData** clientArchives, int argc) {

        int srcmlreturncode = 0;
        char ** pp = new char*[argc];
        size_t size;
        for (int i = 0; i < argc; ++i){
            struct srcml_archive* archive;
            /* create a new srcml archive structure */
            archive = srcml_archive_create();
            SetArchiveData(archive, clientArchives[i]);
            /* open a srcML archive for output */
            srcml_archive_write_open_memory(archive, &pp[i], &size);
            for (int k = 0; k < clientArchives[i]->unitCount; ++k){
                struct srcml_unit* unit;
                unit = srcml_unit_create(archive);

                /*Set all srcML options provided through clientArchives*/
                SetUnitData(unit, &clientArchives[i]->units[k]);

                /*Set filename for unit*/
                srcml_unit_set_filename(unit, clientArchives[i]->units[k].filename);

                /*Parse*/
                srcmlreturncode = srcml_unit_parse_memory(unit, clientArchives[i]->units[k].buffer, clientArchives[i]->units[k].bufferSize);

                /* Translate to srcml and append to the archive */
                srcml_write_unit(archive, unit);
                srcml_unit_free(unit);
                if (srcmlreturncode){
                    srcml_archive_close(archive);
                    srcml_archive_free(archive);
                    Exception^ error = gcnew Exception(String::Format("could not parse file {0}. SrcML returned with status {1}", gcnew String(clientArchives[i]->units[k].filename), srcmlreturncode));
                    throw error;
                }
            }
            /* close the srcML archive */
            srcml_archive_close(archive);

            /* free the srcML archive data */
            srcml_archive_free(archive);

            /*Trim any garbage data from the end of the string. TODO: Error check*/
            LibSrcMLWrapper::TrimFromEnd(pp[i], size);
        }
        return pp;
    }
}