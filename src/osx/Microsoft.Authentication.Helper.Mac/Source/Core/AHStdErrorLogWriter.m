// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#import "AHStdErrorLogWriter.h"

@implementation AHStdErrorLogWriter

- (void)writeMessage:(NSString *)message {
    NSError* error;
    [message writeToFile:@"/dev/stderr" atomically:NO encoding:NSUTF8StringEncoding error:&error];
}

@end
