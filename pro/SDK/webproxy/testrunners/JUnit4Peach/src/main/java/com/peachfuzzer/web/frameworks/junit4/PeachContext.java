package com.peachfuzzer.web.frameworks.junit4;

import com.peachfuzzer.api.PeachState;
import com.peachfuzzer.api.PeachApiException;
import com.peachfuzzer.api.Proxy;

public class PeachContext {
    public Proxy proxy;
    public PeachState state;
    public PeachApiException ex;
    public Boolean debug = false;
    
    public PeachContext()
    {
        proxy = new Proxy();
        state = PeachState.CONTINUE;
    }
}
