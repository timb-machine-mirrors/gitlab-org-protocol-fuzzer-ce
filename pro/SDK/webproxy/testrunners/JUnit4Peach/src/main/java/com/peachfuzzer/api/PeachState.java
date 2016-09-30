
package com.peachfuzzer.api;

/**
 * FOR FUTURE USE
 * State as returned by Proxy API
 */
public enum PeachState {
    /** Continue with current test case */
    CONTINUE(1),
    /** Skip to next test case */
    NEXT_TESTCASE(2),
    /** Stop testing */
    STOP(3),
    /** Internal, stop error occurred */
    ERROR(4);
    
    private int numVal;

    PeachState(int numVal) {
        this.numVal = numVal;
    }

    public int getNumVal() {
        return numVal;
    }
}

