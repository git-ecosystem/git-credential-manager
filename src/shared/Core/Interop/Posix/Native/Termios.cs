using System;

namespace GitCredentialManager.Interop.Posix.Native
{
    [Flags]
    public enum SetActionFlags
    {
        /// <summary>
        /// Make change immediately.
        /// </summary>
        TCSANOW = 0,

        /// <summary>
        /// Drain output, then change.
        /// </summary>
        TCSADRAIN = 1,

        /// <summary>
        /// Drain output, flush input.
        /// </summary>
        TCSAFLUSH = 2,
    }

    [Flags]
    public enum InputFlags : uint
    {
        IGNBRK  = 0x00000001, // ignore BREAK condition
        BRKINT  = 0x00000002, // map BREAK to SIGINTR
        IGNPAR  = 0x00000004, // ignore (discard) parity errors
        PARMRK  = 0x00000008, // mark parity and framing errors
        INPCK   = 0x00000010, // enable checking of parity errors
        ISTRIP  = 0x00000020, // strip 8th bit off chars
        INLCR   = 0x00000040, // map NL into CR
        IGNCR   = 0x00000080, // ignore CR
        ICRNL   = 0x00000100, // map CR to NL (ala CRMOD)
        IXON    = 0x00000200, // enable output flow control
        IXOFF   = 0x00000400, // enable input flow control
        IXANY   = 0x00000800, // any char will restart after stop
        IMAXBEL = 0x00002000, // ring bell on input queue full
        IUTF8   = 0x00004000, // maintain state for UTF-8 VERASE
    }

    [Flags]
    public enum OutputFlags : uint
    {
        OPOST  = 0x00000001, // enable following output processing
        ONLCR  = 0x00000002, // map NL to CR-NL (ala CRMOD)
        OXTABS = 0x00000004, // expand tabs to spaces
        ONOEOT = 0x00000008, // discard EOT's (^D) on output)
        OCRNL  = 0x00000010, // map CR to NL on output
        ONOCR  = 0x00000020, // no CR output at column 0
        ONLRET = 0x00000040, // NL performs CR function
        OFILL  = 0x00000080, // use fill characters for delay
        NLDLY  = 0x00000300, // \n delay
        TABDLY = 0x00000c04, // horizontal tab delay
        CRDLY  = 0x00003000, // \r delay
        FFDLY  = 0x00004000, // form feed delay
        BSDLY  = 0x00008000, // \b delay
        VTDLY  = 0x00010000, // vertical tab delay
        OFDEL  = 0x00020000, // fill is DEL, else NUL
    }

    [Flags]
    public enum ControlFlags : uint
    {
        CIGNORE    = 0x00000001, // ignore control flags
        CSIZE      = 0x00000300, // character size mask
        CS5        = 0x00000000, // 5 bits (pseudo)
        CS6        = 0x00000100, // 6 bits
        CS7        = 0x00000200, // 7 bits
        CS8        = 0x00000300, // 8 bits
        CSTOPB     = 0x00000400, // send 2 stop bits
        CREAD      = 0x00000800, // enable receiver
        PARENB     = 0x00001000, // parity enable
        PARODD     = 0x00002000, // odd parity, else even
        HUPCL      = 0x00004000, // hang up on last close
        CLOCAL     = 0x00008000, // ignore modem status lines
        CCTS_OFLOW = 0x00010000, // CTS flow control of output
        CRTSCTS    = (CCTS_OFLOW | CRTS_IFLOW),
        CRTS_IFLOW = 0x00020000, // RTS flow control of input
        CDTR_IFLOW = 0x00040000, // DTR flow control of input
        CDSR_OFLOW = 0x00080000, // DSR flow control of output
        CCAR_OFLOW = 0x00100000, // DCD flow control of output
        MDMBUF     = 0x00100000, // old name for CCAR_OFLOW
    }

    [Flags]
    public enum LocalFlags : uint
    {
        ECHOKE     = 0x00000001, // visual erase for line kill
        ECHOE      = 0x00000002, // visually erase chars
        ECHOK      = 0x00000004, // echo NL after line kill
        ECHO       = 0x00000008, // enable echoing
        ECHONL     = 0x00000010, // echo NL even if ECHO is off
        ECHOPRT    = 0x00000020, // visual erase mode for hardcopy
        ECHOCTL    = 0x00000040, // echo control chars as ^(Char)
        ISIG       = 0x00000080, // enable signals INTR, QUIT, [D]SUSP
        ICANON     = 0x00000100, // canonicalize input lines
        ALTWERASE  = 0x00000200, // use alternate WERASE algorithm
        IEXTEN     = 0x00000400, // enable DISCARD and LNEXT
        EXTPROC    = 0x00000800, // external processing
        TOSTOP     = 0x00400000, // stop background jobs from output
        FLUSHO     = 0x00800000, // output being flushed (state)
        NOKERNINFO = 0x02000000, // no kernel output from VSTATUS
        PENDIN     = 0x20000000, // XXX retype pending input (state)
        NOFLSH     = 0x80000000, // don't flush after interrupt
    }
}
