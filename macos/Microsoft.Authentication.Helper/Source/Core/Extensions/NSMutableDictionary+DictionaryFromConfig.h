// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#import <Foundation/Foundation.h>

@interface NSMutableDictionary (DictionaryFromConfig)

+ (NSMutableDictionary<NSString*, NSString*>*)dictionaryFromConfig:(NSString*)linesOfConfig;

+ (NSMutableDictionary<NSString*, NSString*>*)dictionaryFromFileHandle:(NSFileHandle*)fileHandle;

@end

