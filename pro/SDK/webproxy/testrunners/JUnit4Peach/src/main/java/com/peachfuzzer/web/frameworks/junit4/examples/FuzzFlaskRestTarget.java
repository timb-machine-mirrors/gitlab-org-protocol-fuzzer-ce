
/*
 * Example of a junit4 setup that runs unit tests through
 * Peach Fuzzer.
 */

package com.peachfuzzer.web.frameworks.junit4.examples;

import com.mashape.unirest.http.options.Option;
import com.mashape.unirest.http.options.Options;
import com.peachfuzzer.web.frameworks.junit4.JUnit4PeachSuiteRunner.PeachSuiteClasses;
import org.apache.http.HttpHost;
import org.junit.AfterClass;
import org.junit.BeforeClass;
import org.junit.runner.RunWith;

@RunWith(com.peachfuzzer.web.frameworks.junit4.JUnit4PeachSuiteRunner.class)
@PeachSuiteClasses(TestFlaskRestTarget.class)
public class FuzzFlaskRestTarget {
    @BeforeClass
    public static void setProxy()
    {
        Options.setOption(Option.PROXY, new HttpHost("127.0.0.1", 8001));
    }
    
    @AfterClass
    public static void removeProxy()
    {
        Options.setOption(Option.PROXY, null);
    }
}
