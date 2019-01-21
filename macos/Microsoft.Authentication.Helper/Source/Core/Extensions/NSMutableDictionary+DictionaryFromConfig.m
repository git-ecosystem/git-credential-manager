// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#import "NSMutableDictionary+DictionaryFromConfig.h"
#define IsValidString(str) ((str) != nil && [(str)length] > 0)

@implementation NSMutableDictionary (DictionaryFromConfig)

+ (NSMutableDictionary<NSString*, NSString*>*)dictionaryFromConfig:(NSString*)linesOfConfig
{
	NSMutableDictionary<NSString*, NSString*> *dictionary = [NSMutableDictionary dictionary];
	
	if (IsValidString(linesOfConfig))
	{
		NSArray* lines = [linesOfConfig componentsSeparatedByCharactersInSet:[NSCharacterSet newlineCharacterSet]];
		for (NSString* line in lines)
		{
			if ([line rangeOfString:@"="].location != NSNotFound)
			{
				NSString *key = [[[line componentsSeparatedByString:@"="] objectAtIndex:0]
								 stringByTrimmingCharactersInSet:[NSCharacterSet whitespaceCharacterSet]];
				NSString *value = [[[line componentsSeparatedByString:@"="] objectAtIndex:1]
								   stringByTrimmingCharactersInSet:[NSCharacterSet whitespaceCharacterSet]];
				
				if (IsValidString(key) && IsValidString(value))
					[dictionary setObject:value forKey:key];
			}
		}
	}
	
	return dictionary;
}

+ (NSMutableDictionary<NSString*, NSString*>*)dictionaryFromFileHandle:(NSFileHandle*)fileHandle;
{
    NSMutableString* allData = [[NSMutableString alloc] init];

    Boolean keepReading = YES;
    while (keepReading)
    {
        NSString* line = [[NSString alloc] initWithData:[fileHandle availableData]
                                               encoding:(NSUTF8StringEncoding)];
        [allData appendString:line];

        // Keep reading until we have a double line-feed character signifying
        // the end of the dictionary.
        NSUInteger dataLength = [allData length];
        if (dataLength > 1)
        {
            unichar lastChar = [allData characterAtIndex:dataLength-1];
            unichar secondLastChar = [allData characterAtIndex:dataLength-2];

            if (lastChar == '\n' && secondLastChar == '\n')
            {
                keepReading = NO;
            }
        }
    }

    return [NSMutableDictionary dictionaryFromConfig:allData];
}

@end
