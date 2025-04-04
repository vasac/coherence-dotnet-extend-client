/*
 * Copyright (c) 2000, 2022, Oracle and/or its affiliates.
 *
 * Licensed under the Universal Permissive License v 1.0 as shown at
 * https://oss.oracle.com/licenses/upl.
 */
package concurrent.atomic;

import com.oracle.coherence.concurrent.atomic.AsyncAtomicBoolean;
import com.oracle.coherence.concurrent.atomic.AsyncRemoteAtomicBoolean;
import com.oracle.coherence.concurrent.atomic.Atomics;
import com.tangosol.net.Coherence;

import org.junit.jupiter.api.AfterAll;
import org.junit.jupiter.api.BeforeAll;

/**
 * Tests for {@link AsyncRemoteAtomicBoolean} using POF.
 *
 * @author Aleks Seovic  2020.12.07
 * @since 21.12
 */
public class AsyncRemoteAtomicBooleanPofTest
        extends AsyncAtomicBooleanTest
    {
    // ----- test lifecycle -------------------------------------------------

    @BeforeAll
    static void startUp()
        {
        System.setProperty("coherence.serializer", "pof");
        System.setProperty("coherence.pof.config", "coherence-concurrent-pof-config.xml");
        Coherence.clusterMember().start().join();
        }

    @AfterAll
    static void shutDown()
        {
        Coherence.closeAll();
        System.clearProperty("coherence.serializer");
        System.clearProperty("coherence.pof.config");
        }

    // ----- AsyncAtomicBooleanTest methods ---------------------------------

    @Override
    protected AsyncAtomicBoolean asyncValue()
        {
        return Atomics.remoteAtomicBoolean("value").async();
        }
    }
