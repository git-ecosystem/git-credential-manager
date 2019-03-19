// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#import <Foundation/Foundation.h>
#import "AHLogWriter.h"

NS_ASSUME_NONNULL_BEGIN

@interface AHFileLogWriter : NSObject<AHLogWriter>
{
    NSString* logFilePath;
}

- (instancetype) initWithPath:(NSString*)logFilePath;
@end

NS_ASSUME_NONNULL_END
