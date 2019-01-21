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
        [self->dateFormatter setDateFormat:@"yyyy/MM/dd HH:mm:ss:SSS"];
    }

    return self;
}

- (void) addWriter:(NSObject<AHLogWriter> *) writer
{
    [self->writers addObject:writer];
}

-(void)log:(NSString*)message
{
    NSString* logMessage = [NSString stringWithFormat:@"%@: %@\n",
                            [dateFormatter stringFromDate:[NSDate date]],
                            message];

    for (NSObject<AHLogWriter> *writer in self->writers)
    {
        [writer writeMessage:logMessage];
    }
}

@end
