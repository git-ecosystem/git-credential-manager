// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#import "AHFileLogWriter.h"

@implementation AHFileLogWriter

- (instancetype) initWithPath:(NSString*)logFilePath
{
    self = [super init];

    if (self)
    {
        self->logFilePath = logFilePath;
    }

    return self;
}

-(void)writeMessage:(NSString*)message
{
    // Open file
    NSFileHandle* handle = [NSFileHandle fileHandleForWritingAtPath:logFilePath];

    if (handle == nil) {
        // Create new file because it does not exist
        [[NSFileManager defaultManager] createFileAtPath:logFilePath contents:nil attributes:nil];
        handle = [NSFileHandle fileHandleForWritingAtPath:logFilePath];
    }

    // Append message
    [handle seekToEndOfFile];
    [handle writeData:[message dataUsingEncoding:NSUTF8StringEncoding]];

    // Close
    [handle closeFile];
}

@end
