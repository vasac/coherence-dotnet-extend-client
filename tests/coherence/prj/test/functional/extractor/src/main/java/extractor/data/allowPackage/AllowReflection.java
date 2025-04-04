/*
 * Copyright (c) 2000, 2022, Oracle and/or its affiliates.
 *
 * Licensed under the Universal Permissive License v 1.0 as shown at
 * https://oss.oracle.com/licenses/upl.
 */

package extractor.data.allowPackage;


import extractor.data.TestInterface;

/**
 * Test class for CoherenceReflectFilterTests.
 *
 * @author jf 2020.05.19
 */
public class AllowReflection
        implements TestInterface
    {
    // ----- constructors ---------------------------------------------------

    public AllowReflection(String sValue)
        {
        m_sProperty = sValue;
        }

    // ----- TestInterface methods ------------------------------------------

    @Override
    public String getProperty()
        {
        return m_sProperty;
        }

    @Override
    public void setProperty(String sValue)
        {
        m_sProperty = sValue;
        }

    // ----- data members ---------------------------------------------------

    private String m_sProperty;
    }
