// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#import "AHLogger.h"

@implementation AHLogger

-(instancetype)init
{
    self = [super init];
    if (self != nil)
    {
        self->writers = [[NSMutableArray<AHLogWriter> alloc] init];
        self->dateFormatter = [[NSDateFormatter alloc] init];

        // Configure the date formatter based on the POSIX locale as "HH" can still return
        // a 12-hour style time if the user's locale has disabled the 24-hour style.
        NSLocale *enUSPOSIXLocale = [[NSLocale alloc] initWithLocaleIdentifier:@"en_US_POSIX"];
        [self->dateFormatter setLocale:enUSPOSIXLocale];
        [self->dateFormatter setDateFormat:@"HH:mm:ss:SSSSSS"];
    }

    return self;
}

- (void) addWriter:(NSObject<AHLogWriter> *) writer
{
    [self->writers addObject:writer];
}

-(void)log:(NSString*)message
  fileName:(NSString*)fileName
   lineNum:(int)lineNum
{
    // To be consistent with Git and GCM's main trace output format we want the source (file:line)
    // column to be 23 characters wide, truncating the start with "..." if required.
    NSString* source = [[NSString alloc] initWithFormat:@"%@:%d", fileName, lineNum];
    if ([source length] > 23)
    {
        NSInteger extra = [source length] - 23 + 3;
        source = [[NSString alloc] initWithFormat:@"...%@", [source substringFromIndex:extra]];
    }

    NSString* logMessage = [NSString stringWithFormat:@"%@ %23s trace: %@\n",
                      [dateFormatter stringFromDate:[NSDate date]],
                            [source UTF8String],
                            message];

    for (NSObject<AHLogWriter> *writer in self->writers)
    {
        [writer writeMessage:logMessage];
    }
}

-(void)log:(NSString*)message
 secretMsg:(NSString*)secretMsg
  fileName:(NSString*)fileName
   lineNum:(int)lineNum;
{
    NSString* combinedMessage;
    if ([self enableSecretTracing])
    {
        combinedMessage = [[NSString alloc] initWithFormat:@"%@ PII: %@",
                           message, secretMsg];
    }
    else
    {
        combinedMessage = [[NSString alloc] initWithFormat:@"%@ PII: ********",
                           message];
    }
    
    [self log:combinedMessage
     fileName:fileName
      lineNum:lineNum];
}

@end
