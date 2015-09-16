#include "stdafx.h"
using System::String;
extern"C"{
    __declspec(dllexport) inline bool TestArchiveSetXmlEncoding(LibSrcMLWrapper::SourceData** sd){
        struct srcml_archive* archive;
        archive = srcml_archive_create();
        srcml_archive_set_xml_encoding(archive, sd[0]->encoding);
        if (srcml_archive_get_xml_encoding(archive)){
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return true;
        }
        else{
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return false;
        }
    }
    __declspec(dllexport) inline bool TestArchiveSetSrcEncoding(LibSrcMLWrapper::SourceData** sd){
        struct srcml_archive* archive;
        archive = srcml_archive_create();
        srcml_archive_set_src_encoding(archive, sd[0]->src_encoding);
        if (srcml_archive_get_src_encoding(archive)){
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return true;
        }
        else{
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return false;
        }
    }
    __declspec(dllexport) inline bool TestArchiveSetLanguage(LibSrcMLWrapper::SourceData** sd){
        struct srcml_archive* archive;
        archive = srcml_archive_create();
        srcml_archive_set_language(archive, sd[0]->language);
        if (srcml_archive_get_language(archive)){
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return true;
        }
        else{
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return false;
        }
    }
    __declspec(dllexport) inline bool TestArchiveSetUrl(LibSrcMLWrapper::SourceData** sd){
        struct srcml_archive* archive;
        archive = srcml_archive_create();
        srcml_archive_set_url(archive, sd[0]->url);
        if (srcml_archive_get_url(archive)){
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return true;
        }
        else{
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return false;
        }
    }
    __declspec(dllexport) inline bool TestArchiveSetVersion(LibSrcMLWrapper::SourceData** sd){
        struct srcml_archive* archive;
        archive = srcml_archive_create();
        srcml_archive_set_version(archive, sd[0]->version);
        if (srcml_archive_get_version(archive)){
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return true;
        }
        else{
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return false;
        }
    }
    __declspec(dllexport) inline bool TestArchiveSetOptions(LibSrcMLWrapper::SourceData** sd){
        struct srcml_archive* archive;
        archive = srcml_archive_create();
        unsigned long opts = srcml_archive_get_options(archive);
        srcml_archive_set_options(archive, sd[0]->optionSet);
        if (srcml_archive_get_options(archive) != opts){
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return true;
        }
        else{
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return false;
        }
    }
    __declspec(dllexport) inline bool TestArchiveEnableOption(LibSrcMLWrapper::SourceData** sd){
        struct srcml_archive* archive;
        archive = srcml_archive_create();
        unsigned long opts = srcml_archive_get_options(archive);
        srcml_archive_enable_option(archive, sd[0]->optionEnable);
        if (srcml_archive_get_options(archive) != opts){
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return true;
        }
        else{
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return false;
        }
    }
    __declspec(dllexport) inline bool TestArchiveDisableOption(LibSrcMLWrapper::SourceData** sd){
        struct srcml_archive* archive;
        archive = srcml_archive_create();
        srcml_archive_enable_option(archive, SRCML_OPTION_LITERAL);
        unsigned long opts = srcml_archive_get_options(archive);
        srcml_archive_disable_option(archive, sd[0]->optionDisable);
        if (srcml_archive_get_options(archive) != opts){
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return true;
        }
        else{
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return false;
        }
    }
    __declspec(dllexport) inline bool TestArchiveSetTabstop(LibSrcMLWrapper::SourceData** sd){
        struct srcml_archive* archive;
        archive = srcml_archive_create();
        srcml_archive_set_tabstop(archive, sd[0]->tabstop);
        if (srcml_archive_get_tabstop(archive) == sd[0]->tabstop){
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return true;
        }
        else{
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return false;
        }
    }
    __declspec(dllexport) inline bool TestArchiveRegisterFileExtension(LibSrcMLWrapper::SourceData** sd){
        struct srcml_archive* archive;
        archive = srcml_archive_create();
        int stat = srcml_archive_register_file_extension(archive, sd[0]->extandlanguage[0], sd[0]->extandlanguage[1]);
        if (stat) return false;
        const char* lang = srcml_archive_check_extension(archive, "file.h");
        if (std::strcmp(lang, SRCML_LANGUAGE_CXX) == 0){
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return true;
        }
        srcml_archive_close(archive);
        srcml_archive_free(archive);
        return false;
    }
    __declspec(dllexport) inline bool TestArchiveRegisterNamespace(LibSrcMLWrapper::SourceData** sd){
        struct srcml_archive* archive;
        archive = srcml_archive_create();
        srcml_archive_register_namespace(archive, sd[0]->prefixandnamespace[0], sd[0]->prefixandnamespace[1]);
        const char* theuri = srcml_archive_get_uri_from_prefix(archive, "abb");
        if (std::strcmp(theuri, "www.abb.com") == 0){
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return true;
        }
        else{
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return false;
        }
    }
    __declspec(dllexport) inline bool TestArchiveSetProcessingInstruction(LibSrcMLWrapper::SourceData** sd){
        struct srcml_archive* archive;
        archive = srcml_archive_create();
        srcml_archive_set_processing_instruction(archive, sd[0]->targetanddata[0], sd[0]->targetanddata[1]);
        if (!srcml_archive_get_processing_instruction_target(archive)){
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return false;
        }
        if (!srcml_archive_get_processing_instruction_data(archive)){
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return false;
        }
        srcml_archive_close(archive);
        srcml_archive_free(archive);
        return true;
    }
    __declspec(dllexport) inline bool TestArchiveRegisterMacro(LibSrcMLWrapper::SourceData** sd){
        struct srcml_archive* archive;
        archive = srcml_archive_create();
        srcml_archive_register_macro(archive, sd[0]->tokenandtype[0], sd[0]->tokenandtype[1]);
        if (srcml_archive_get_macro_list_size(archive)){
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return true;
        }
        else{
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return false;
        }
    }
    __declspec(dllexport) inline bool TestUnitSetFilename(LibSrcMLWrapper::SourceData** sd){
        struct srcml_archive *archive;
        archive = srcml_archive_create();
        struct srcml_unit *unit;
        unit = srcml_unit_create(archive);
        srcml_unit_set_filename(unit, sd[0]->filename[0]);
        const char* ufname = srcml_unit_get_filename(unit);
        if (std::strcmp(sd[0]->filename[0], ufname) == 0){
            srcml_unit_free(unit);
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return true;
        }
        else{
            srcml_unit_free(unit);
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return false;
        }
    }
    __declspec(dllexport) inline bool TestUnitSetLanguage(LibSrcMLWrapper::SourceData** sd){
        struct srcml_archive *archive;
        archive = srcml_archive_create();
        struct srcml_unit *unit;
        unit = srcml_unit_create(archive);
        srcml_unit_set_language(unit, sd[0]->language);
        const char* lang = srcml_unit_get_language(unit);
        if (std::strcmp(sd[0]->language, lang) == 0){
            srcml_unit_free(unit);
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return true;
        }
        else{
            srcml_unit_free(unit);
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return false;
        }
    }
    __declspec(dllexport) inline bool TestUnitSetSrcEncoding(LibSrcMLWrapper::SourceData** sd){
        struct srcml_archive *archive;
        archive = srcml_archive_create();
        struct srcml_unit *unit;
        unit = srcml_unit_create(archive);
        srcml_unit_set_src_encoding(unit, sd[0]->src_encoding);
        if (std::strcmp(srcml_unit_get_src_encoding(unit), "UTF-8") == 0){
            srcml_unit_free(unit);
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return true;
        }
        else{
            srcml_unit_free(unit);
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return false;
        }
    }
    __declspec(dllexport) inline bool TestUnitSetUrl(LibSrcMLWrapper::SourceData** sd){
        struct srcml_archive *archive;
        archive = srcml_archive_create();
        struct srcml_unit *unit;
        unit = srcml_unit_create(archive);
        srcml_unit_set_url(unit, sd[0]->url);
        if (std::strcmp(srcml_unit_get_url(unit), "www.abb.com") == 0){
            srcml_unit_free(unit);
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return true;
        }
        else{
            srcml_unit_free(unit);
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return false;
        }
    }
    __declspec(dllexport) inline bool TestUnitSetVersion(LibSrcMLWrapper::SourceData** sd){
        struct srcml_archive *archive;
        archive = srcml_archive_create();
        struct srcml_unit *unit;
        unit = srcml_unit_create(archive);
        srcml_unit_set_version(unit, sd[0]->version);
        if (std::strcmp(srcml_unit_get_version(unit), "1.0") == 0){
            srcml_unit_free(unit);
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return true;
        }
        else{
            srcml_unit_free(unit);
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return false;
        }
    }
    __declspec(dllexport) inline bool TestUnitSetTimestamp(LibSrcMLWrapper::SourceData** sd){
        struct srcml_archive *archive;
        archive = srcml_archive_create();
        struct srcml_unit *unit;
        unit = srcml_unit_create(archive);
        srcml_unit_set_timestamp(unit, sd[0]->timestamp);
        if (std::strcmp(srcml_unit_get_timestamp(unit), "0800") == 0){
            srcml_unit_free(unit);
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return true;
        }
        else{
            srcml_unit_free(unit);
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return false;
        }
    }
    __declspec(dllexport) inline bool TestUnitSetHash(LibSrcMLWrapper::SourceData** sd){
        struct srcml_archive *archive;
        archive = srcml_archive_create();
        struct srcml_unit *unit;
        unit = srcml_unit_create(archive);
        srcml_unit_set_hash(unit, sd[0]->hash);
        if (std::strcmp(srcml_unit_get_hash(unit), "hash") == 0){
            srcml_unit_free(unit);
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return true;
        }
        else{
            srcml_unit_free(unit);
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return false;
        }
    }
    __declspec(dllexport) inline bool TestUnitUnparseSetEol(LibSrcMLWrapper::SourceData** sd){
        struct srcml_archive *archive;
        archive = srcml_archive_create();
        struct srcml_unit *unit;
        unit = srcml_unit_create(archive);
        if (srcml_unit_unparse_set_eol(unit, sd[0]->eol)){
            srcml_unit_free(unit);
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return true;
        }
        else{
            srcml_unit_free(unit);
            srcml_archive_close(archive);
            srcml_archive_free(archive);
            return false;
        }
    }
}