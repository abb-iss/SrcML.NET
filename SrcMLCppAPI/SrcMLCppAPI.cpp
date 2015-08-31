// This is the main DLL file.

#include "stdafx.h"
#include "srcml.h"
#include <iostream>
#include <string>
#include "Windows.h"
using namespace System;
using namespace System::Runtime::InteropServices;
extern"C"{
	__declspec(dllexport) int SrcmlCreateArchiveFromFilename(char** argv, int argc, const char* outputFile){
		int i;
		struct srcml_archive* archive;
		struct srcml_unit* unit;
		
		String^ clistr = gcnew String(outputFile);
		char* str2 = (char*)(void*)Marshal::StringToHGlobalAnsi(clistr);
		
		/* create a new srcml archive structure */
		archive = srcml_archive_create();

		/* open a srcML archive for output */
		srcml_archive_write_open_filename(archive, str2, 0);

		//Marshal::FreeHGlobal(str2);

		/* add all the files to the archive */
		for (i = 0; i < argc; ++i) {
			String^ clistr = gcnew String(argv[i]);
			char* str2 = (char*)(void*)Marshal::StringToHGlobalAnsi(clistr);
			Console::WriteLine(clistr);

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

		return 0;
	}
	__declspec(dllexport) char* SrcmlCreateArchiveFromMemory(char** argv, int argc){
		int i;
		LPDWORD n;
		struct srcml_archive* archive;
		struct srcml_unit* unit;
		char * s;
		size_t size;
		HANDLE srcml_input;

		/* create a new srcml archive structure */
		archive = srcml_archive_create();

		/* open a srcML archive for output */
		srcml_archive_write_open_memory(archive, &s, &size);

		/* add all the files to the archive */
		for (i = 1; i < argc; ++i) {

			unit = srcml_unit_create(archive);

			/* Translate to srcml and append to the archive */
			char buffer[256];
			srcml_input = CreateFile((LPCWSTR)argv[i], OF_READ, 0, NULL, NULL, FILE_ATTRIBUTE_NORMAL, NULL);
			int num_read = ReadFile(srcml_input, buffer, 256, n, 0);
			CloseHandle(srcml_input);
			srcml_unit_set_language(unit, srcml_archive_check_extension(archive, argv[i]));

			srcml_unit_parse_memory(unit, buffer, num_read);

			/* Translate to srcml and append to the archive */
			srcml_write_unit(archive, unit);

			srcml_unit_free(unit);
		}

		/* close the srcML archive */
		srcml_archive_close(archive);

		/* free the srcML archive data */
		srcml_archive_free(archive);

		return s;
	}
}