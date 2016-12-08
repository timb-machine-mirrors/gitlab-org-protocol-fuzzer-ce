package com.peachfuzzer.web.frameworks.junit4.examples.selenium;


import com.mashape.unirest.http.options.Option;
import com.mashape.unirest.http.options.Options;
import org.apache.http.HttpHost;
import org.junit.AfterClass;
import org.junit.BeforeClass;
import org.junit.runner.RunWith;
import org.junit.runners.Suite;

@RunWith(Suite.class)
@Suite.SuiteClasses({
    RestTargetGetallusers.class,
    RestTargetCreateuser.class,
    RestTargetUpdateuser.class})
public class TestSeleniumFlaskRestTarget {
    @BeforeClass
    public static void setProxy()
    {
        System.setProperty("webdriver.gecko.driver", "c:\\temp\\geckodriver.exe");
        Options.setOption(Option.PROXY, new HttpHost("127.0.0.1", 8001));
    }
    
}
