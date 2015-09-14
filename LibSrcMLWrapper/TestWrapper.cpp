#include "stdafx.h"

extern"C"{
    int test_archive_set_xml_encoding(LibSrcMLWrapper::SourceData* sd){
        struct srcml_archive* archive;
        archive = srcml_archive_create();
        srcml_archive_set_src_encoding(archive, sd->src_encoding);
        assert(srcml_archive_get_src_encoding(archive));
        srcml_archive_close(archive);
        srcml_archive_free(archive);
        return 0;
    }
    int test_archive_set_src_encoding(LibSrcMLWrapper::SourceData* sd){
        struct srcml_archive* archive;
        archive = srcml_archive_create();
        srcml_archive_set_xml_encoding(archive, sd->encoding);
        assert(srcml_archive_get_xml_encoding(archive));
        srcml_archive_close(archive);
        srcml_archive_free(archive);
        return 0;
    }
    int test_archive_set_language(LibSrcMLWrapper::SourceData* sd){
        struct srcml_archive* archive;
        archive = srcml_archive_create();
        srcml_archive_set_language(archive, sd->language);
        assert(srcml_archive_get_language(archive));
        srcml_archive_close(archive);
        srcml_archive_free(archive);
        return 0;
    }
    int test_archive_set_url(LibSrcMLWrapper::SourceData* sd){
        struct srcml_archive* archive;
        archive = srcml_archive_create();
        srcml_archive_set_url(archive, sd->url);
        assert(srcml_archive_get_url(archive));
        srcml_archive_close(archive);
        srcml_archive_free(archive);
        return 0;
    }
    int test_archive_set_version(LibSrcMLWrapper::SourceData* sd){
        struct srcml_archive* archive;
        archive = srcml_archive_create();
        srcml_archive_set_version(archive, sd->version);
        assert(srcml_archive_get_version(archive));
        srcml_archive_close(archive);
        srcml_archive_free(archive);
        return 0;
    }
    int test_archive_set_options(LibSrcMLWrapper::SourceData* sd){
        struct srcml_archive* archive;
        archive = srcml_archive_create();
        srcml_archive_set_options(archive, sd->optionSet);
        assert(srcml_archive_get_options(archive));
        srcml_archive_close(archive);
        srcml_archive_free(archive);
        return 0;
    }
    int test_archive_enable_option(LibSrcMLWrapper::SourceData* sd){
        struct srcml_archive* archive;
        archive = srcml_archive_create();
        srcml_archive_enable_option(archive, sd->optionEnable);
        assert(srcml_archive_get_options(archive));
        srcml_archive_close(archive);
        srcml_archive_free(archive);
        return 0;
    }
    int test_archive_disable_option(LibSrcMLWrapper::SourceData* sd){
        struct srcml_archive* archive;
        archive = srcml_archive_create();
        srcml_archive_disable_option(archive, sd->optionDisable);
        assert(srcml_archive_get_options(archive));
        srcml_archive_close(archive);
        srcml_archive_free(archive);
        return 0;
    }
    int test_archive_set_tabstop(LibSrcMLWrapper::SourceData* sd){
        struct srcml_archive* archive;
        archive = srcml_archive_create();
        srcml_archive_set_tabstop(archive, sd->tabstop);
        assert(srcml_archive_get_tabstop(archive));
        srcml_archive_close(archive);
        srcml_archive_free(archive);
        return 0;
    }
    int test_archive_register_file_extension(LibSrcMLWrapper::SourceData* sd){
        struct srcml_archive* archive;
        archive = srcml_archive_create();
        srcml_archive_register_file_extension(archive, sd->extandlanguage[0], sd->extandlanguage[1]);
        //srcml_archive_check_extension(archive, const char* filename);
        srcml_archive_close(archive);
        srcml_archive_free(archive);
        return 0;
    }
    int test_archive_register_namespace(LibSrcMLWrapper::SourceData* sd){
        struct srcml_archive* archive;
        archive = srcml_archive_create();
        srcml_archive_register_namespace(archive, sd->prefixandnamespace[0], sd->prefixandnamespace[1]);
        assert(srcml_archive_get_namespace_size(archive));
        //assert(srcml_archive_get_uri_from_prefix(archive, prefix));
        srcml_archive_close(archive);
        srcml_archive_free(archive);
        return 0;
    }
    int test_archive_set_processing_instruction(LibSrcMLWrapper::SourceData* sd){
        struct srcml_archive* archive;
        archive = srcml_archive_create();
        srcml_archive_set_processing_instruction(archive, sd->targetanddata[0], sd->targetanddata[1]);
        assert(srcml_archive_get_processing_instruction_target(archive));
        assert(srcml_archive_get_processing_instruction_data(archive));
        srcml_archive_close(archive);
        srcml_archive_free(archive);
        return 0;
    }
    int test_archive_register_macro(LibSrcMLWrapper::SourceData* sd){
        struct srcml_archive* archive;
        archive = srcml_archive_create();
        srcml_archive_register_macro(archive, sd->tokenandtype[0], sd->tokenandtype[1]);
        assert(srcml_archive_get_macro_list_size(archive));
        //assert(srcml_archive_get_macro_token_type(archive, type));
        srcml_archive_close(archive);
        srcml_archive_free(archive);
        return 0;
    }
}