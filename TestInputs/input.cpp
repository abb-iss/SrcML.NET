extern"C"{
    /// <summary>
    /// This creates an archive from a list of files and saves to a file
    /// </summary>
    ///<param name="argv">List of files to be read</param>
    ///<param name="argc">Number of arguments in argv</param>
    ///<param name="outputFile">File to output to</param>
    __declspec(dllexport) int SrcmlCreateArchiveFtF(LibSrcMLWrapper::SourceData** sd, int argc, const char* outputFile) {
        struct srcml_archive* archive;
        struct srcml_unit* unit;
        int srcmlreturncode = -1;
        for (int i = 0; i < argc; ++i){
            /*create a new srcml archive structure */
            archive = srcml_archive_create();
            SetArchiveData(archive, sd[i]);

            std::string filename(outputFile);
            filename += std::to_string(i) + ".cpp.xml";

            /*open a srcML archive for output */
            srcml_archive_write_open_filename(archive, filename.c_str(), 0);

            /* add all the files to the archive */
            for (int k = 0; k < sd[i]->buffercount; ++k) {

                unit = srcml_unit_create(archive);

                /*Set all srcML options provided through sd*/
                SetUnitData(unit, sd[i]);

                /*Set filename for unit*/
                srcml_unit_set_filename(unit, sd[i]->filename[k]);

                /*Translate to srcml and append to the archive */
                srcmlreturncode = srcml_unit_parse_filename(unit, sd[i]->filename[k]);

                /*Translate to srcml and append to the archive */
                srcml_write_unit(archive, unit);

                srcml_unit_free(unit);
                if (srcmlreturncode){
                    srcml_archive_close(archive);
                    srcml_archive_free(archive);
                    Exception^ error = gcnew Exception(String::Format("could not parse file {0}. SrcML returned with status {1}", gcnew String(sd[i]->filename[k]), srcmlreturncode));
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
    __declspec(dllexport) int SrcmlCreateArchiveMtF(LibSrcMLWrapper::SourceData** sd, int argc, const char* outputFile) {
        struct srcml_archive* archive;
        struct srcml_unit* unit;
        int srcmlreturncode = -1;
        for (int i = 0; i < argc; ++i){
            /* create a new srcml archive structure */
            archive = srcml_archive_create();
            SetArchiveData(archive, sd[i]);

            /* open a srcML archive for output */
            std::string filename(outputFile);
            filename += std::to_string(i) + ".cpp.xml";

            /*open a srcML archive for output */
            srcml_archive_write_open_filename(archive, filename.c_str(), 0);

            for (int j = 0; j < sd[i]->buffercount; ++j){

                /* add all the files to the archive */

                unit = srcml_unit_create(archive);

                /*Set all srcML options provided through sd*/
                SetUnitData(unit, sd[i]);

                /*Set filename for unit*/
                srcml_unit_set_filename(unit, sd[i]->filename[j]);

                /*Parse*/
                srcmlreturncode = srcml_unit_parse_memory(unit, sd[i]->buffer[j], sd[i]->buffersize[j]);

                /* Translate to srcml and append to the archive */
                srcml_write_unit(archive, unit);
                srcml_unit_free(unit);
                if (srcmlreturncode){
                    srcml_archive_close(archive);
                    srcml_archive_free(archive);
                    Exception^ error = gcnew Exception(String::Format("could not parse file {0}. SrcML returned with status {1}", gcnew String(sd[i]->filename[j]), srcmlreturncode));
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
    __declspec(dllexport) char** SrcmlCreateArchiveFtM(LibSrcMLWrapper::SourceData** sd, int argc) {
        char** pp = new char*[2];
        size_t size;
        int srcmlreturncode = 0;
        /* add all the files to the archive */
        for (int i = 0; i < argc; ++i) {
            /* create a new srcml archive structure */
            struct srcml_archive* archive;
            archive = srcml_archive_create();
            SetArchiveData(archive, sd[i]);

            /* open a srcML archive for output */
            srcml_archive_write_open_memory(archive, &pp[i], &size);
            for (int k = 0; k < sd[i]->buffercount; ++k){
                struct srcml_unit* unit;
                unit = srcml_unit_create(archive);

                /*Set all srcML options provided through sd*/
                SetUnitData(unit, sd[i]);

                /*Set filename for unit*/
                srcml_unit_set_filename(unit, sd[i]->filename[k]);
                std::pair<char*, std::streamoff> bufferPair;
                try{
                    //Read file into pair of c-string and size of the file. TODO: Error check
                    bufferPair = LibSrcMLWrapper::ReadFileC(sd[i]->filename[k]);

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
                    Exception^ error = gcnew Exception(String::Format("could not parse file {0}. SrcML returned with status {1}", gcnew String(sd[i]->filename[k]), srcmlreturncode));
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
    __declspec(dllexport) char** SrcmlCreateArchiveMtM(LibSrcMLWrapper::SourceData** sd, int argc) {

        int srcmlreturncode = 0;
        char ** pp = new char*[argc];
        size_t size;
        for (int i = 0; i < argc; ++i){
            struct srcml_archive* archive;
            /* create a new srcml archive structure */
            archive = srcml_archive_create();
            SetArchiveData(archive, sd[i]);
            /* open a srcML archive for output */
            srcml_archive_write_open_memory(archive, &pp[i], &size);
            for (int k = 0; k < sd[i]->buffercount; ++k){
                struct srcml_unit* unit;
                unit = srcml_unit_create(archive);

                /*Set all srcML options provided through sd*/
                SetUnitData(unit, sd[i]);

                /*Set filename for unit*/
                srcml_unit_set_filename(unit, sd[i]->filename[k]);

                /*Parse*/
                srcmlreturncode = srcml_unit_parse_memory(unit, sd[i]->buffer[k], sd[i]->buffersize[k]);

                /* Translate to srcml and append to the archive */
                srcml_write_unit(archive, unit);
                srcml_unit_free(unit);
                if (srcmlreturncode){
                    srcml_archive_close(archive);
                    srcml_archive_free(archive);
                    Exception^ error = gcnew Exception(String::Format("could not parse file {0}. SrcML returned with status {1}", gcnew String(sd[i]->filename[k]), srcmlreturncode));
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